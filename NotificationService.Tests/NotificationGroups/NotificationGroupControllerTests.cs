using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NotificationService.Api.Controllers.Groups;
using NotificationService.Application.Commands.Groups;
using NotificationService.Application.Queries.Groups;
using NotificationService.Domain.Entities;
using TaskHandler.Shared.Notifications.DTOs.Groups;
using TaskHandler.Shared.Notifications.DTOs.Groups.Responses;

namespace NotificationService.Tests.Controllers;

public class NotificationGroupControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly NotificationGroupController _controller;

    public NotificationGroupControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        var configurationMock = new Mock<IConfiguration>();
        var loggerMock = new Mock<ILogger<NotificationGroupController>>();

        // Setup configuration mock
        configurationMock.Setup(c => c["AuthSettings:BaseUrl"])
            .Returns("https://auth.example.com");

        // Setup HttpClient mock
        var httpClientMock = new Mock<HttpClient>();
        httpClientFactoryMock.Setup(f => f.CreateClient("AuthService"))
            .Returns(httpClientMock.Object);

        _controller = new NotificationGroupController(
            _mediatorMock.Object,
            httpClientFactoryMock.Object,
            configurationMock.Object,
            loggerMock.Object);

        // Setup user context
        SetupUserContext("test-user-id");
    }

    private void SetupUserContext(string? userId)
    {
        ClaimsPrincipal claimsPrincipal;
        
        if (userId == null)
        {
            claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity());
        }
        else
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim("sub", userId),
                new Claim("userId", userId)
            };

            var identity = new ClaimsIdentity(claims, "Test");
            claimsPrincipal = new ClaimsPrincipal(identity);
        }

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = claimsPrincipal
            }
        };
    }

    #region GetGroupsForUser Tests

    [Fact]
    public async Task GetGroupsForUser_WithValidUserId_ReturnsOkResult()
    {
        // Arrange
        var userId = "test-user-id";
        var expectedGroups = new List<NotificationGroupEntity>
        {
            NotificationGroupEntity.Create("Test Group", "Test Description", userId, new List<string> { userId })
        };
        
        var queryResponse = new GetGroupsForUserQueryResponse(true, expectedGroups);

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetGroupsForUserQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(queryResponse);

        // Act
        var result = await _controller.GetGroupsForUser(userId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);

        _mediatorMock.Verify(m => m.Send(It.Is<GetGroupsForUserQuery>(q => q.Id == userId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetGroupsForUser_WithEmptyUserId_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.GetGroupsForUser("");

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("User id is not provided", badRequestResult.Value);

        _mediatorMock.Verify(m => m.Send(It.IsAny<GetGroupsForUserQuery>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetGroupsForUser_WithNullUserId_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.GetGroupsForUser(null);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("User id is not provided", badRequestResult.Value);
    }

    [Fact]
    public async Task GetGroupsForUser_WhenQueryFails_ReturnsBadRequest()
    {
        // Arrange
        var userId = "test-user-id";
        var queryResponse = new GetGroupsForUserQueryResponse(false, null);

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetGroupsForUserQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(queryResponse);

        // Act
        var result = await _controller.GetGroupsForUser(userId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Can't get notification groups for user", badRequestResult.Value);
    }

    [Fact]
    public async Task GetGroupsForUserFromCookie_WithValidUser_ReturnsOkResult()
    {
        // Arrange
        var expectedGroups = new List<NotificationGroupEntity>
        {
            NotificationGroupEntity.Create("Test Group", "Test Description", "test-user-id", new List<string> { "test-user-id" })       
        };
        
        var queryResponse = new GetGroupsForUserQueryResponse(true, expectedGroups);

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetGroupsForUserQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(queryResponse);

        // Act
        var result = await _controller.GetGroupsForUser();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetGroupsForUserFromCookie_WithNoUser_ReturnsUnauthorized()
    {
        // Arrange
        SetupUserContext(null);

        // Act
        var result = await _controller.GetGroupsForUser();

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    #endregion

    #region CreateGroup Tests

    [Fact]
    public async Task CreateGroup_WithValidData_ReturnsOkResult()
    {
        // Arrange
        var dto = new CreateNotificationGroupDto
        {
            Name = "Test Group",
            Description = "Test Description",
            Members = new List<string> { "user1", "user2" }
        };

        var commandResponse = new CreateNotificationGroupResponse(true, "Group created successfully");
        
        _mediatorMock.Setup(m => m.Send(It.IsAny<CreateNotificationGroupCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(commandResponse);

        // Act
        var result = await _controller.CreateGroup(dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<CreateNotificationGroupResponse>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("Group created successfully", response.Message);
    }

    [Fact]
    public async Task CreateGroup_WithNoUser_ReturnsUnauthorized()
    {
        // Arrange
        SetupUserContext(null);
        
        var dto = new CreateNotificationGroupDto
        {
            Name = "Test Group",
            Description = "Test Description",
            Members = new List<string> { "user1", "user2" }
        };

        // Act
        var result = await _controller.CreateGroup(dto);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        var response = Assert.IsType<CreateNotificationGroupResponse>(unauthorizedResult.Value);
        Assert.False(response.Success);
        Assert.Equal("User is not authorized", response.Message);
    }

    [Fact]
    public async Task CreateGroup_WhenCommandFails_ReturnsBadRequest()
    {
        // Arrange
        var dto = new CreateNotificationGroupDto
        {
            Name = "Test Group",
            Description = "Test Description",
            Members = new List<string> { "user1", "user2" }
        };

        var commandResponse = new CreateNotificationGroupResponse(false, "Error creating group");

        _mediatorMock.Setup(m => m.Send(It.IsAny<CreateNotificationGroupCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(commandResponse);

        // Act
        var result = await _controller.CreateGroup(dto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<CreateNotificationGroupResponse>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Equal("Error creating group", response.Message);
    }

    #endregion

    #region UpdateGroup Tests

    [Fact]
    public async Task UpdateGroup_WithValidData_ReturnsOkResult()
    {
        // Arrange
        var dto = new UpdateGroupDto
        {
            GroupId = "group-id",
            Name = "Updated Group",
            Description = "Updated Description"
        };

        var commandResponse = new UpdateGroupCommandResponse(true, "Group updated successfully");

        _mediatorMock.Setup(m => m.Send(It.IsAny<UpdateGroupCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(commandResponse);

        // Act
        var result = await _controller.UpdateGroup(dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<UpdateGroupResponseDto>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("Group updated successfully", response.Message);
    }
    
    #endregion
}