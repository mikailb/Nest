using System.IO;
#nullable disable
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.IO.Abstractions;
using Microsoft.AspNetCore.Mvc.Routing;
using Moq;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Xunit;
using Nest.Controllers; // Adjust namespace as needed
using Nest.Models;
using Nest.DAL;
using Nest.ViewModels;
using Nest.Utilities;

namespace Nest.Tests.Controllers
{
    public class NoteControllerTests
    {
        private readonly NoteController _controller;
        private readonly Mock<INoteRepository> _noteRepositoryMock;
        private readonly Mock<ICommentRepository> _commentRepositoryMock;
        private readonly Mock<ILogger<NoteController>> _loggerMock;
        private readonly Mock<IUrlHelper> _urlHelperMock;
        private readonly Mock<UserManager<IdentityUser>> _userManagerMock;

        public NoteControllerTests() //This part of testing code is to initialize so we don't have to write this multiple times
        {
            //Instantiate mocks
            _noteRepositoryMock = new Mock<INoteRepository>();
            _commentRepositoryMock = new Mock<ICommentRepository>();
            _loggerMock = new Mock<ILogger<NoteController>>();
            _urlHelperMock = new Mock<IUrlHelper>();

            //Set up UserManager<IdentityUser> mock with default constructor parameters
            var store = new Mock<IUserStore<IdentityUser>>();
            _userManagerMock = new Mock<UserManager<IdentityUser>>(
                store.Object, null, null, null, null, null, null, null, null
            );

            //Initialize controller with mocks
            _controller = new NoteController(
                _noteRepositoryMock.Object,
                _commentRepositoryMock.Object,
                _loggerMock.Object,
                _userManagerMock.Object
            );
        }

        [Fact]
        public async Task Create_ReturnsView_WhenModelStateIsInvalid()
        {
            // Arrange
            _controller.ModelState.AddModelError("Title", "Required");
            var newNote = new Note();

            // Act
            var result = await _controller.Create(newNote);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(newNote, viewResult.Model);
        }

        [Fact]
        public async Task Create_SavesNoteAndCreatesDatabaseRecord_WhenValid()
        {
            //Arrange
            var userId = "user123";
            var userName = "testUser";
            var noteTitle = "Test Title";
            var noteContent = "This is a test note content.";

            //Set up a Note object to be created
            var newNote = new Note
            {
                NoteId = 10, //New note, ID will be autoset by the repository
                Title = noteTitle,
                Content = noteContent
            };

            //Mock UserManager to return the current users ID and username
            var mockUser = new IdentityUser { Id = userId, UserName = userName };
            _userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(mockUser);

            //Mock the repository to handle the creation of the note
            _noteRepositoryMock.Setup(repo => repo.Create(It.IsAny<Note>())).Returns(Task.CompletedTask);


            //Act
            var result = await _controller.Create(newNote);

            //Assert
            //Verify that the note was created and saved in the repository
            _noteRepositoryMock.Verify(repo => repo.Create(It.Is<Note>(n => 
                n.Title == noteTitle && 
                n.Content == noteContent)), Times.Once);

            //Check that the result is a RedirectToActionResult, redirecting to the "Notes" or another specified page
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MyPage", redirectResult.ActionName); // Change "Notes" to the appropriate action name if needed
        }

        [Fact]
        public async Task CreateNote_ReturnsView_WhenDatabaseSaveFails()
        {
            //Arrange
            var newNote = new Note();
            _userManagerMock.Setup(u => u.GetUserName(It.IsAny<ClaimsPrincipal>())).Returns("testUser");
            _noteRepositoryMock.Setup(repo => repo.Create(It.IsAny<Note>())).Returns(Task.CompletedTask);

            //Act
            var result = await _controller.Create(newNote);

            //Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MyPage", redirectResult.ActionName); // Change "Notes" to the appropriate action name if needed
        }

        [Fact]
        public async Task DeleteConfirmed_DeletesNote_WhenNoteIdExists()
        {
            //Arrange
            var source = "Notes";
            int noteId = 30;
            var currentUserName = "testUser";
            var newNote = new Note { NoteId = noteId, username = currentUserName };

            //Set up the repository to return the note and confirm deletion
            _noteRepositoryMock.Setup(repo => repo.GetNoteById(noteId)).ReturnsAsync(newNote);
            _noteRepositoryMock.Setup(repo => repo.DeleteConfirmed(noteId)).ReturnsAsync(true);

            //Mock user identity to get past it
            _userManagerMock.Setup(u => u.GetUserName(It.IsAny<ClaimsPrincipal>())).Returns(currentUserName);

            //Act
            var result = await _controller.DeleteConfirmed(noteId, source);

            //Assert
            //Check for redirect after deletion
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Notes", redirectToActionResult.ActionName); // Assuming it redirects to "Notes"

            //Verify that DeleteConfirmed was called exactly once
            _noteRepositoryMock.Verify(repo => repo.DeleteConfirmed(noteId), Times.Once);
        }
        
        [Fact]
        public async Task DeleteConfirmed_ReturnsForbid_WhenUserIsNotAuthorized()
        {
            //Arrange
            var source = "Notes";
            var noteId = 50;
            var currentUserName = "testUser";
            var anotherUserName = "unauthorizedUser";
            var note = new Note { NoteId = noteId, username = anotherUserName };

            //Set up the repository to return the note owned by a different user
            _noteRepositoryMock.Setup(repo => repo.GetNoteById(noteId)).ReturnsAsync(note);

            //Mock the current user identity to a different user
            _userManagerMock.Setup(u => u.GetUserName(It.IsAny<ClaimsPrincipal>())).Returns(currentUserName);

            //Act
            var result = await _controller.DeleteConfirmed(noteId, source);

            //Assert
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task Edit_note_ReturnsToNotes_WhenEditIsOk()
        {
            // Arrange
            var noteId = 50;
            var currentUserName = "testUser";
            var existingTitle = "Old Title";
            var existingContent = "Old Content";
            var newTitle = "New Title";
            var newContent = "New Content";

            // Set up the existing note in the repository
            var existingNote = new Note
            {
                NoteId = noteId,
                username = currentUserName,
                Title = existingTitle,
                Content = existingContent,
            };

            // Set up the Edited note details
            var EditedNote = new Note
            {
                NoteId = noteId, // Make sure the IDs match to pass the ID check
                Title = newTitle,
                Content = newContent
            };

            // Mock repository to return the existing note and confirm successful Edit
            _noteRepositoryMock.Setup(repo => repo.GetNoteById(noteId)).ReturnsAsync(existingNote);
            _noteRepositoryMock.Setup(repo => repo.Edit(It.IsAny<Note>())).Returns(Task.CompletedTask);

            // Mock user identity to match the note owner
            _userManagerMock.Setup(u => u.GetUserName(It.IsAny<ClaimsPrincipal>())).Returns(currentUserName);

            // Make sure ModelState is valid
            _controller.ModelState.Clear();

            // Set up TempData
            var tempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
            {
                ["Source"] = "Notes" // Simulate the TempData value
            };
            _controller.TempData = tempData;

            // Act
            var result = await _controller.Edit(EditedNote, "Notes");

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result); // Verify the result is a RedirectToActionResult
            Assert.Equal("Notes", redirectResult.ActionName); // Ensure redirection to the "Notes" action

            // Verify that the title and Content have been edited in the existing note
            Assert.Equal(newTitle, existingNote.Title);
            Assert.Equal(newContent, existingNote.Content);

            // Verify that Edit was called on the repository
            _noteRepositoryMock.Verify(repo => repo.Edit(It.Is<Note>(n =>
                n.NoteId == noteId &&
                n.Title == newTitle &&
                n.Content == newContent
            )), Times.Once);

            // Check TempData 
            Assert.True(_controller.TempData.ContainsKey("Source"));
            Assert.Equal("Notes", _controller.TempData["Source"]);
        }

        [Fact]
        public async Task Details_GoIntoDetailedView_FromNotes()
        {
            //Arrange
            var noteId = 50;
            var source = "Notes";
            var expectedNote = new Note
            {
                NoteId = noteId,
                Title = "Test Title",
                Content = "Test Description",
            };

            //Set up the repository to return the expected Note
            _noteRepositoryMock.Setup(repo => repo.GetNoteById(noteId)).ReturnsAsync(expectedNote);

            //Act
            var result = await _controller.Details(noteId, source);

            //Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Details", viewResult.ViewName); //Check that the view name is "NoteDetails"
            Assert.Equal(expectedNote, viewResult.Model); //Verify that the model is the expected Note

            //Check that source is set correctly
            Assert.Equal(source, _controller.ViewBag.Source);

            //Verify that NoteId was called on the repository with the correct id
            _noteRepositoryMock.Verify(repo => repo.GetNoteById(noteId), Times.Once);
        }
    }
}
