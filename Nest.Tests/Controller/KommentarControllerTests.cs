using System.IO;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.IO.Abstractions;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Xunit;
using Nest.Controllers; // Adjust namespace as needed
using Nest.Models;
using Nest.DAL;
using Nest.ViewModels;
using Nest.Utilities;
#nullable disable


namespace Nest.Tests.Controllers;

public class CommentControllerTests
{
    private readonly Mock<IPictureRepository> _pictureRepositoryMock;
    private readonly Mock<ICommentRepository> _commentRepositoryMock;
    private readonly Mock<INoteRepository> _noteRepositoryMock;
    private readonly Mock<ILogger<CommentController>> _loggerMock;
    private readonly Mock<UserManager<IdentityUser>> _userManagerMock;
    private readonly CommentController _controller;

    public CommentControllerTests() //This part of testing code is to initialize so we don't have to write this multiple times
    {
        _commentRepositoryMock = new Mock<ICommentRepository>(); 
        _pictureRepositoryMock = new Mock<IPictureRepository>();
        _noteRepositoryMock = new Mock<INoteRepository>();
        _loggerMock = new Mock<ILogger<CommentController>>();

        //Set up UserManager<IdentityUser> mock with default constructor parameters
        var store = new Mock<IUserStore<IdentityUser>>();
        _userManagerMock = new Mock<UserManager<IdentityUser>>(
            store.Object, null, null, null, null, null, null, null, null);


        //Pass all five required parameters to the CommentController constructor
        _controller = new CommentController(
            _commentRepositoryMock.Object, 
            _loggerMock.Object,
            _userManagerMock.Object);
    }

    [Fact]
    public async Task CreateCommentForPicture_SaveCommentInDB_Verifies()
    {
        //Arrange
        var testComment = new Comment
        {
            CommentId = 1,
            PictureId = 10,
            CommentDescription = "This is a test comment",
            CommentTime = DateTime.UtcNow,
            UserName = "TestUser"
        };

        _commentRepositoryMock
            .Setup(repo => repo.Create(It.IsAny<Comment>()))
            .Returns(Task.CompletedTask); // Simulate repository behavior

        //Act
        var result = await _controller.CreateComment(testComment);

        //Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Grid", redirectResult.ActionName); // Change "Grid" to the appropriate action name if needed

        _commentRepositoryMock.Verify(repo => repo.Create(It.Is<Comment>(n => 
                n.CommentId == testComment.CommentId && 
                n.PictureId == testComment.PictureId && n.NoteId == null)), Times.Once); //NoteId has to be null
    }

    [Fact]
    public async Task CreateCommentForNotes_SaveCommentInDB_Verifies()
    {
        //Arrange
        var testComment = new Comment
        {
            CommentId = 1,
            NoteId = 10,
            CommentDescription = "This is a test comment",
            CommentTime = DateTime.UtcNow,
            UserName = "TestUser"
        };

        _commentRepositoryMock
            .Setup(repo => repo.Create(It.IsAny<Comment>()))
            .Returns(Task.CompletedTask); //Simulate repository behavior

        //Act
        var result = await _controller.CreateCommentNote(testComment);

        //Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.True(
            redirectResult.ActionName == "MyPage" || redirectResult.ActionName == "Notes",
            "Redirection should be either to 'MyPage' or 'Notes'"
        );

        _commentRepositoryMock.Verify(repo => repo.Create(It.Is<Comment>(n =>
            n.CommentId == testComment.CommentId &&
            n.NoteId == testComment.NoteId &&
            n.PictureId == null //PictureId has to be null as per controller logic
        )), Times.Once);
    }

    [Fact]
    public async Task Create_ReturnsView_WhenModelStateIsInvalid()
    {
        // Arrange
        _controller.ModelState.AddModelError("Content", "Required");
        var newComment = new Comment();

        // Act
        var result = await _controller.CreateComment(newComment);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(newComment, viewResult.Model);
    }

    [Fact]
    public async Task DeleteCommentForPictures_DBConfirmed_WhenCommentIdExist()
    {
        //Arrange
        var testComment = new Comment
        {
            CommentId = 1,
            PictureId = 10,
            CommentDescription = "This is a test comment",
            CommentTime = DateTime.UtcNow,
            UserName = "AuthorizedUser"
        };

        _commentRepositoryMock
            .Setup(repo => repo.GetCommentById(It.IsAny<int>()))
            .ReturnsAsync(testComment); //Mock the repository to return the comment

        _userManagerMock
            .Setup(userManager => userManager.GetUserName(It.IsAny<ClaimsPrincipal>()))
            .Returns("AuthorizedUser"); //Mock the user as the owner of the comment

        //Initialize TempData (required for the controller to set TempData["Source"])
        var tempData = new Mock<ITempDataDictionary>();
        _controller.TempData = tempData.Object;

        //Act
        var result = await _controller.DeleteComment(testComment.CommentId);

        //Assert
        var viewResult = Assert.IsType<ViewResult>(result); // Verify it returns a ViewResult
        Assert.Equal(testComment, viewResult.Model); // Verify the model passed to the view is correct

        _commentRepositoryMock.Verify(repo => repo.GetCommentById(1), Times.Once); // Ensure GetCommentById is called once
        _userManagerMock.Verify(userManager => userManager.GetUserName(It.IsAny<ClaimsPrincipal>()), Times.Once); // Ensure user identity check happens
    }

    [Fact]
    public async Task DeleteCommentNote_ReturnsView_WhenUserIsAuthorized()
    {
        // Arrange
        var testComment = new Comment
        {
            CommentId = 1,
            NoteId = 10,
            CommentDescription = "This is a test comment",
            CommentTime = DateTime.UtcNow,
            UserName = "AuthorizedUser"
        };

        _commentRepositoryMock
            .Setup(repo => repo.GetCommentById(It.IsAny<int>()))
            .ReturnsAsync(testComment); // Mock the repository to return the comment

        _userManagerMock
            .Setup(userManager => userManager.GetUserName(It.IsAny<ClaimsPrincipal>()))
            .Returns("AuthorizedUser"); // Mock the user as the owner of the comment

        // Initialize TempData (required for the controller to set TempData["Source"])
        var tempData = new Mock<ITempDataDictionary>();
        _controller.TempData = tempData.Object;

        // Act
        var result = await _controller.DeleteCommentNote(testComment.CommentId);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result); // Verify it returns a ViewResult
        Assert.Equal(testComment, viewResult.Model); // Verify the model passed to the view is correct

        _commentRepositoryMock.Verify(repo => repo.GetCommentById(1), Times.Once); // Ensure GetCommentById is called once
        _userManagerMock.Verify(userManager => userManager.GetUserName(It.IsAny<ClaimsPrincipal>()), Times.Once); // Ensure user identity check happens
    }

    [Fact]
    public async Task DeleteCommentNote_ReturnsForbid_WhenUserIsUnauthorized()
    {
        // Arrange
        var testComment = new Comment
        {
            CommentId = 1,
            NoteId = 10,
            CommentDescription = "This is a test comment",
            CommentTime = DateTime.UtcNow,
            UserName = "AuthorizedUser" // The owner of the comment
        };

        _commentRepositoryMock
            .Setup(repo => repo.GetCommentById(It.IsAny<int>()))
            .ReturnsAsync(testComment); // Mock the repository to return the comment

        _userManagerMock
            .Setup(userManager => userManager.GetUserName(It.IsAny<ClaimsPrincipal>()))
            .Returns("UnauthorizedUser"); // Mock the user as someone else

        // Act
        var result = await _controller.DeleteCommentNote(testComment.CommentId);

        // Assert
        var forbidResult = Assert.IsType<ForbidResult>(result); // Verify it returns a ForbidResult

        _commentRepositoryMock.Verify(repo => repo.GetCommentById(1), Times.Once); // Ensure GetCommentById is called
        _userManagerMock.Verify(userManager => userManager.GetUserName(It.IsAny<ClaimsPrincipal>()), Times.Once); // Ensure user identity check happens
    }

    [Fact]
    public async Task DeleteCommentPicture_ReturnsForbid_WhenUserIsUnauthorized()
    {
        //Arrange
        var testComment = new Comment
        {
            CommentId = 1,
            PictureId = 10,
            CommentDescription = "This is a test comment",
            CommentTime = DateTime.UtcNow,
            UserName = "AuthorizedUser" //The owner of the comment
        };

        _commentRepositoryMock
            .Setup(repo => repo.GetCommentById(It.IsAny<int>()))
            .ReturnsAsync(testComment); //Mock the repository to return the comment

        _userManagerMock
            .Setup(userManager => userManager.GetUserName(It.IsAny<ClaimsPrincipal>()))
            .Returns("UnauthorizedUser"); //Mock the user as someone else

        //Act
        var result = await _controller.DeleteCommentNote(testComment.CommentId);

        //Assert
        var forbidResult = Assert.IsType<ForbidResult>(result); //Verify it returns a ForbidResult

        _commentRepositoryMock.Verify(repo => repo.GetCommentById(1), Times.Once); //Ensure GetCommentById is called
        _userManagerMock.Verify(userManager => userManager.GetUserName(It.IsAny<ClaimsPrincipal>()), Times.Once); //Ensure user identity check happens
    }

    [Fact]
    public async Task EditCommentNote_RedirectsToNotes_WhenEditIsSuccessful()
    {
        //Arrange
        var commentId = 25;
        var noteId = 50;
        var currentUserName = "testUser";
        var existingDescription = "Old comment description";
        var updatedDescription = "Updated comment description";

        //Set up the existing comment in the repository
        var existingComment = new Comment
        {
            CommentId = commentId,
            NoteId = noteId,
            UserName = currentUserName,
            CommentDescription = existingDescription,
            CommentTime = DateTime.UtcNow
        };

        //Set up the updated comment details
        var updatedComment = new Comment
        {
            CommentId = commentId, //IDs must match
            CommentDescription = updatedDescription
        };

        //Mock repository to return the existing comment and confirm successful edit
        _commentRepositoryMock.Setup(repo => repo.GetCommentById(commentId)).ReturnsAsync(existingComment);
        _commentRepositoryMock.Setup(repo => repo.Edit(It.IsAny<Comment>())).ReturnsAsync(true);

        //Mock user identity to match the comment owner
        _userManagerMock.Setup(u => u.GetUserName(It.IsAny<ClaimsPrincipal>())).Returns(currentUserName);

        //Make sure ModelState is valid
        _controller.ModelState.Clear();

        //Set up TempData
        var tempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            ["Source"] = "Notes" // Simulate the TempData value
        };
        _controller.TempData = tempData;

        //Act
        var result = await _controller.EditCommentNote(commentId, updatedComment, "Notes");

        //Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result); // Verify the result is a RedirectToActionResult
        Assert.Equal("Notes", redirectResult.ActionName); // Ensure redirection to the "Notes" action
        Assert.Equal("Note", redirectResult.ControllerName); // Ensure the controller name is "Note"
        Assert.Equal(noteId, redirectResult.RouteValues["id"]); // Ensure the redirection includes the correct note ID

        //Verify that the comment description and timestamp were updated
        Assert.Equal(updatedDescription, existingComment.CommentDescription);
        Assert.True(existingComment.CommentTime > DateTime.UtcNow.AddSeconds(-5), "CommentTime should be updated to the current time");

        //Verify that Edit was called on the repository
        _commentRepositoryMock.Verify(repo => repo.Edit(It.Is<Comment>(c =>
            c.CommentId == commentId &&
            c.CommentDescription == updatedDescription
        )), Times.Once);

        //Check TempData
        Assert.True(_controller.TempData.ContainsKey("Source"));
        Assert.Equal("Notes", _controller.TempData["Source"]);
    }

    [Fact]
    public async Task EditCommentPicture_RedirectsToGrid_WhenEditIsSuccessful()
    {
        //Arrange
        var commentId = 25;
        var pictureId = 50; //Assume the comment belongs to a picture
        var currentUserName = "testUser";
        var existingDescription = "Old comment description";
        var updatedDescription = "Updated comment description";

        //Set up the existing comment in the repository
        var existingComment = new Comment
        {
            CommentId = commentId,
            PictureId = pictureId, // Specific to a picture
            UserName = currentUserName,
            CommentDescription = existingDescription,
            CommentTime = DateTime.UtcNow
        };

        //Set up the updated comment details
        var updatedComment = new Comment
        {
            CommentId = commentId, // IDs must match
            CommentDescription = updatedDescription
        };

        //Mock repository to return the existing comment and confirm successful edit
        _commentRepositoryMock.Setup(repo => repo.GetCommentById(commentId)).ReturnsAsync(existingComment);
        _commentRepositoryMock.Setup(repo => repo.Edit(It.IsAny<Comment>())).ReturnsAsync(true);

        //Mock user identity to match the comment owner
        _userManagerMock.Setup(u => u.GetUserName(It.IsAny<ClaimsPrincipal>())).Returns(currentUserName);

        //Make sure ModelState is valid
        _controller.ModelState.Clear();

        //Set up TempData
        var tempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            ["Source"] = "Grid" // Simulate the TempData value
        };
        _controller.TempData = tempData;

        //Act
        var result = await _controller.EditComment(commentId, updatedComment, "Grid");

        //Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result); // Verify the result is a RedirectToActionResult
        Assert.Equal("Grid", redirectResult.ActionName); // Ensure redirection to the "Grid" action
        Assert.Equal("Picture", redirectResult.ControllerName); // Ensure the controller name is "Picture"

        //Verify that the comment description and timestamp were updated
        Assert.Equal(updatedDescription, existingComment.CommentDescription);
        Assert.True(existingComment.CommentTime > DateTime.UtcNow.AddSeconds(-5), "CommentTime should be updated to the current time");

        //Verify that Edit was called on the repository
        _commentRepositoryMock.Verify(repo => repo.Edit(It.Is<Comment>(c =>
            c.CommentId == commentId &&
            c.CommentDescription == updatedDescription
        )), Times.Once);

        //Check TempData
        Assert.True(_controller.TempData.ContainsKey("Source"));
        Assert.Equal("Grid", _controller.TempData["Source"]);
    }
    
    [Fact]
    public async Task CreateComment_ReturnsView_WhenDatabaseSaveFails()
    {
        //Arrange
        var newComment = new Comment();
        _userManagerMock.Setup(u => u.GetUserName(It.IsAny<ClaimsPrincipal>())).Returns("testUser");
        _commentRepositoryMock.Setup(repo => repo.Create(It.IsAny<Comment>())).Returns(Task.CompletedTask);

        //Act
        var result = await _controller.CreateCommentNote(newComment, "Notes");

        //Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Notes", redirectResult.ActionName); // Change "Notes" to the appropriate action name if needed
    }


}