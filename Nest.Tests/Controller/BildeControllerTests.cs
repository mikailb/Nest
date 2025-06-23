using System.IO;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.IO.Abstractions;
using Microsoft.AspNetCore.Mvc.Routing;
using Moq;
using Xunit;
using Nest.Controllers; // Adjust namespace as needed
using Nest.Models;
using Nest.DAL;
using Nest.ViewModels;
using Nest.Utilities;
#nullable disable


namespace Nest.Tests.Controllers;

public class PictureControllerTests
{
    private readonly Mock<IPictureRepository> _pictureRepositoryMock;
    private readonly Mock<ICommentRepository> _commentRepositoryMock;
    private readonly Mock<ILogger<PictureController>> _loggerMock;
    private readonly Mock<UserManager<IdentityUser>> _userManagerMock;
    private readonly Mock<IUrlHelper> _urlHelperMock;
    private readonly PictureController _controller;

    public PictureControllerTests() //This part of testing code is to initialize so we don't have to write this multiple times
    {
        _pictureRepositoryMock = new Mock<IPictureRepository>();
        _commentRepositoryMock = new Mock<ICommentRepository>(); 
        _loggerMock = new Mock<ILogger<PictureController>>();

        //Set up UserManager<IdentityUser> mock with default constructor parameters
        var store = new Mock<IUserStore<IdentityUser>>();
        _userManagerMock = new Mock<UserManager<IdentityUser>>(
            store.Object, null, null, null, null, null, null, null, null);

        _urlHelperMock = new Mock<IUrlHelper>();


        //Pass all four required parameters to the PictureController constructor
        _controller = new PictureController(
            _pictureRepositoryMock.Object,
            _commentRepositoryMock.Object, 
            _loggerMock.Object,
            _userManagerMock.Object);
    }

    [Fact]
    public async Task Details_GoIntoDetailedView_FromGrid()
    {
    //Arrange
    var imageId = 50;
    var source = "Grid";
    var expectedPicture = new Picture
    {
        PictureId = imageId,
        Title = "Test Title",
        Description = "Test Description",
        PictureUrl = "/images/testImage.jpg"
    };

    //Set up the repository to return the expected Picture
    _pictureRepositoryMock.Setup(repo => repo.PictureId(imageId)).ReturnsAsync(expectedPicture);

    //Act
    var result = await _controller.Details(imageId, source);

    //Assert
    var viewResult = Assert.IsType<ViewResult>(result);
    Assert.Equal("PictureDetails", viewResult.ViewName); // Check that the view name is "PictureDetails"
    Assert.Equal(expectedPicture, viewResult.Model); // Verify that the model is the expected Picture

    //Check that the source is set correctly
    Assert.Equal(source, _controller.ViewBag.Source);

    //Verify that PictureId was called on the repository with the correct id
    _pictureRepositoryMock.Verify(repo => repo.PictureId(imageId), Times.Once);
    }

    [Fact]
    public async Task Create_ReturnsView_WhenModelStateIsInvalid()
    {
        //Arrange
        _controller.ModelState.AddModelError("Title", "Required");
        var newImage = new Picture(); //Make an invalid ModelState on purpose

        //Act
        var result = await _controller.Create(newImage, null);

        //Assert
        var viewResult = Assert.IsType<ViewResult>(result); //Expect that it will return the view
        Assert.Equal(newImage, viewResult.Model);
    }

    [Fact]
    public async Task Create_SavesImageFileAndCreatesDatabaseRecord_WhenValid()
    {
        //Arrange
        var newImage = new Picture();
        var mockFile = new Mock<IFormFile>();
        var fileName = "test.jpg";
        mockFile.Setup(f => f.FileName).Returns(fileName);
        mockFile.Setup(f => f.Length).Returns(1024); //Set a file size

        //Setting up a mock file stream
        using (var ms = new MemoryStream())
        {
            mockFile.Setup(f => f.OpenReadStream()).Returns(ms);
            mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), default)).Returns(Task.CompletedTask);

            var userName = "testUser";
            _userManagerMock.Setup(u => u.GetUserName(It.IsAny<ClaimsPrincipal>())).Returns(userName);

            _pictureRepositoryMock.Setup(repo => repo.Create(It.IsAny<Picture>())).ReturnsAsync(true);

            //Act
            var result = await _controller.Create(newImage, mockFile.Object);

            //Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MyPage", redirectResult.ActionName);
            Assert.StartsWith("/images/", newImage.PictureUrl); //Names get changed, so we made sure that its atleast an image
            Assert.EndsWith(".jpg", newImage.PictureUrl); //IDs are already matching, so it can't duplicate
            _pictureRepositoryMock.Verify(repo => repo.Create(It.Is<Picture>(b => b == newImage)), Times.Once); //Check if it's in the repo
        }
    }

    [Fact]
    public async Task Create_ReturnsView_WhenDatabaseSaveFails()
    {
        //Arrange
        var newImage = new Picture();
        var mockFile = new Mock<IFormFile>();
        _userManagerMock.Setup(u => u.GetUserName(It.IsAny<ClaimsPrincipal>())).Returns("testUser");
        _pictureRepositoryMock.Setup(repo => repo.Create(It.IsAny<Picture>())).ReturnsAsync(false); //Make the database not work on purpose

        //Act
        var result = await _controller.Create(newImage, mockFile.Object);

        //Assert
        var viewResult = Assert.IsType<ViewResult>(result); //Expects the view
        Assert.Equal(newImage, viewResult.Model);
    }

    [Fact]
    public async Task DeleteConfirmed_DeletesFile_WhenFileExists()
    {
        //Arrange
        var imageId = 50;
        var PictureUrl = "/images/test.jpg";
        var currentUserName = "testUser";
        var returnUrl = "Grid";
        var Picture = new Picture { PictureId = imageId, PictureUrl = PictureUrl, UserName = currentUserName };
        
        //Set up the repository to return the image and confirm deletion
        _pictureRepositoryMock.Setup(repo => repo.PictureId(imageId)).ReturnsAsync(Picture);
        _pictureRepositoryMock.Setup(repo => repo.Delete(imageId)).ReturnsAsync(true);
        
        //Mock user identity
        _userManagerMock.Setup(u => u.GetUserName(It.IsAny<ClaimsPrincipal>())).Returns(currentUserName);

        _urlHelperMock.Setup(u => u.Action(It.IsAny<UrlActionContext>()))
            .Returns(returnUrl);
        _controller.Url = _urlHelperMock.Object;

        
        //Set up the delegates to simulate file existence and track deletion
        bool fileDeleted = false;
        FileUtil.FileExists = path => true; // Simulate that the file exists
        FileUtil.FileDelete = path => fileDeleted = true; // Track if deletion occurs

        //Act
        var result = await _controller.DeleteConfirmed(imageId, "Grid");

        //Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result); //Expect RedirectResult
        Assert.Equal(returnUrl, redirectResult.ActionName); //Check that the URL matches the expected returnUrl
        Assert.True(fileDeleted); //Verify that the file was deleted
        _pictureRepositoryMock.Verify(repo => repo.Delete(imageId), Times.Once);
    }

    [Fact]
    public async Task DeleteConfirmed_ReturnsNotFound_WhenPictureDoesNotExist()
    {
        //Arrange
        var imageId = 100;

        //Set up the repository to return null for a non-existing image
        _pictureRepositoryMock.Setup(repo => repo.PictureId(imageId)).ReturnsAsync((Picture)null);

        //Act
        var result = await _controller.DeleteConfirmed(imageId, "Grid");

        //Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteConfirmed_ReturnsForbid_WhenUserIsNotAuthorized()
    {
        //Arrange
        var imageId = 50;
        var PictureUrl = "/images/test.jpg";
        var currentUserName = "testUser";
        var anotherUserName = "unauthorizedUser";
        var Picture = new Picture { PictureId = imageId, PictureUrl = PictureUrl, UserName = anotherUserName };

        //Set up the repository to return the image owned by a different user
        _pictureRepositoryMock.Setup(repo => repo.PictureId(imageId)).ReturnsAsync(Picture);

        //Mock the current user identity to a different user
        _userManagerMock.Setup(u => u.GetUserName(It.IsAny<ClaimsPrincipal>())).Returns(currentUserName);

        //Act
        var result = await _controller.DeleteConfirmed(imageId, "Grid");

        //Assert
        Assert.IsType<ForbidResult>(result);
    }

    //Reset FileUtil delegates after each test to avoid side effects in other tests
    [Fact]
    public void Dispose()
    {
        FileUtil.FileExists = System.IO.File.Exists;
        FileUtil.FileDelete = System.IO.File.Delete;
    }

    [Fact]
    public async Task Edit_image_ReturnsToGrid_WhenEditIsOk()
    {
        //Arrange
        var imageId = 50;
        var currentUserName = "testUser";
        var existingTitle = "Old Title";
        var existingDescription = "Old Description";
        var newTitle = "New Title";
        var newDescription = "New Description";

        //Set up the existing image in the repository
        var existingPicture = new Picture
        {
            PictureId = imageId,
            UserName = currentUserName,
            Title = existingTitle,
            Description = existingDescription,
            PictureUrl = "/images/oldImage.jpg"
        };

        //Set up the Editd image details
        var EditPicture = new Picture
        {
            PictureId = imageId, //Ensure the IDs match to pass the ID check
            Title = newTitle,
            Description = newDescription
        };

        //Mock repository to return the existing image and confirm successful Edit
        _pictureRepositoryMock.Setup(repo => repo.PictureId(imageId)).ReturnsAsync(existingPicture);
        _pictureRepositoryMock.Setup(repo => repo.Edit(existingPicture)).ReturnsAsync(true);

        //Mock user identity to match the image owner
        _userManagerMock.Setup(u => u.GetUserName(It.IsAny<ClaimsPrincipal>())).Returns(currentUserName);

        //Make sure ModelState is valid
        _controller.ModelState.Clear();

        //Act
        var result = await _controller.Edit(imageId, EditPicture, null, "Grid");

        //Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Grid", redirectResult.ActionName); //Make sure redirection to the "Grid" action

        //Verify that the title and description have been Edited in the existing image
        Assert.Equal(newTitle, existingPicture.Title);
        Assert.Equal(newDescription, existingPicture.Description);

        //Verify that edit was called on the repository
        _pictureRepositoryMock.Verify(repo => repo.Edit(existingPicture), Times.Once);
    }


}

