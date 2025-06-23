using Microsoft.AspNetCore.Mvc;
using Nest.Models;
using Nest.DAL;
using Nest.ViewModels;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace Nest.Controllers
{
    public class CommentController : Controller
    {
        private readonly ICommentRepository _CommentRepository;
        private readonly ILogger<CommentController> _logger;
        private readonly UserManager<IdentityUser> _userManager;


        public CommentController(ICommentRepository CommentRepository, ILogger<CommentController> logger, UserManager<IdentityUser> userManager)
        {
            _CommentRepository = CommentRepository;
            _logger = logger;
            _userManager = userManager;
        }
        [HttpGet]
        public IActionResult CreateComment(int pictureId) //Method for creating a comment for chosen pictureID
        {
            try
            {
                var Comment = new Comment
                {
                    PictureId = pictureId //Sets with pictureID it inherits

                };
                return View(Comment); //Returns to view after creating comment for picture id
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to upload comment");
                throw;
            }
        }
        [HttpPost]
        [Authorize] //Method for creating a comment for chosen pictureID
        public async Task<IActionResult> CreateComment(Comment comment, string source = "Grid")
        {
            try
            {
                if (ModelState.IsValid) //Checking if the model is valid
                {
                    comment.CommentTime = DateTime.Now;
                    comment.UserName = _userManager.GetUserName(User);
                    await _CommentRepository.Create(comment);

                    //Redirect based on the source value
                    if (source == "MyPage")
                    {
                        return RedirectToAction("MyPage", "Picture");
                    }
                    else
                    {
                        return RedirectToAction("Grid", "Picture");
                    }
                }

                _logger.LogWarning("[CommentController] Failed to upload comment, ModelState not working");
                return View(comment); //Redirects you to source
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error during comment upload");
                throw;
            }
        }


        [HttpGet]
        [Authorize]
        public async Task<IActionResult> EditComment(int Id, string source = "Grid") //Method for editing comment
        {
            var comment = await _CommentRepository.GetCommentById(Id); //Using repository method to get a comment by its id

            if (comment == null) //If not found, error message will appear
            {
                _logger.LogError("[CommentController] Could not find comment with id {Id}", Id);
                return NotFound();
            }

            var currentUserName = _userManager.GetUserName(User); //Has to be authorized
            if (comment.UserName != currentUserName)
            {
                _logger.LogWarning("Unauthorized edit attempt by user {UserName} for comment {CommentId}", currentUserName, Id);
                return Forbid();
            }

            TempData["Source"] = source; //Store source in TempData for later use in view
            return View(comment);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> EditComment(int Id, Comment updatedComment, string source) //Method for editing comment
        {
            if (Id != updatedComment.CommentId || !ModelState.IsValid) //Has to check that the model is valid and ID is correct
            {
                TempData["Source"] = source; //Preserve source value in case of validation error
                return View(updatedComment);
            }

            var existingComment = await _CommentRepository.GetCommentById(Id); //Get the specified comment
            if (existingComment == null) 
            {
                _logger.LogError("Could not find comment ID {CommentId}", updatedComment.CommentId);
                return NotFound();
            }

            var currentUserName = _userManager.GetUserName(User); //Check if user is authenticated
            if (existingComment.UserName != currentUserName)
            {
                _logger.LogWarning("Unauthorized edit attempt by user {UserName} for comment {CommentId}", currentUserName, updatedComment.CommentId);
                return Forbid();
            }

            //Update the comment content and timestamp after editing
            existingComment.CommentDescription = updatedComment.CommentDescription;
            existingComment.CommentTime = DateTime.Now;

            bool success = await _CommentRepository.Edit(existingComment);
            if (success)
            {
                //Redirect to the correct page based on the Source parameter
                return RedirectToAction(source == "MyPage" ? "MyPage" : "Grid", "Picture");
            }
            else
            {
                _logger.LogWarning("[CommentController] Could not update the comment.");
                TempData["Source"] = source; //Preserve source value in case of update failure
                return View(updatedComment);
            }
        }




        [HttpGet]
        [Authorize]
        public async Task<IActionResult> DeleteComment(int id, string source = "Grid") //Method for deleting a comment
        {
            var comment = await _CommentRepository.GetCommentById(id); //Get the comment you want to delete

            if (comment == null) //If comment is not found
            {
                _logger.LogWarning("Comment not found when trying to delete, comment ID: {CommentId}", id);
                return NotFound();
            }

            var currentUserName = _userManager.GetUserName(User); //Check if user is authenticated
            if (comment.UserName != currentUserName)
            {
                _logger.LogWarning("Unauthorized delete attempt by user {UserName} for comment {CommentId}", currentUserName, id);
                return Forbid();
            }

            //Store the source in TempData for later use in the POST method.
            TempData["Source"] = source;

            return View(comment); //Returns to source
        }


        [HttpPost]
        [Authorize]
        public async Task<IActionResult> DeleteConfirmedComment(int id, string source) //Method for finally deleting the comment
        {
            var comment = await _CommentRepository.GetCommentById(id); 
            if (comment == null)
            {
                _logger.LogWarning("[CommentController] Comment with Id {CommentId} not found", id);
                return NotFound();
            }

            var currentUserName = _userManager.GetUserName(User); //Check for authentication
            if (comment.UserName != currentUserName)
            {
                _logger.LogWarning("Unauthorized delete attempt by user {UserName} for comment {CommentId}", currentUserName, id);
                return Forbid();
            }

            bool success = await _CommentRepository.Delete(id); //Return True if successful

            if (!success)
            {
                _logger.LogError("[CommentController] Comment with Id {CommentId} was not deleted successfully", id);
                TempData["Source"] = source; //Preserve source value in case of deletion failure
                return BadRequest("Comment not deleted");
            }

            //Redirect to the correct page based on the Source parameter
            return RedirectToAction(source == "MyPage" ? "MyPage" : "Grid", "Picture");
        }



        //FOR NOTES
        [HttpGet]
        [Authorize]
        public IActionResult CreateCommentNote(int noteId, string source = "Notes") //Method for creating comment for note
        {
            try
            {
                var Comment = new Comment
                {
                    NoteId = noteId
                };
                TempData["Source"] = source; //Store the source in TempData for use after the comment is created.
                return View(Comment);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error create new comment");
                throw;
            }
        }


        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateCommentNote(Comment comment, string source = "Notes") //Method for creating comment for note
        {
            try
            {
                if (ModelState.IsValid)
                {
                    comment.PictureId = null;  //Since this is for Note comments
                    comment.CommentTime = DateTime.Now;
                    comment.UserName = _userManager.GetUserName(User);

                    await _CommentRepository.Create(comment);

                    //Redirect based on source parameter
                    if (source == "MyPage")
                    {
                        return RedirectToAction("MyPage", "Note");
                    }
                    else
                    {
                        return RedirectToAction("Notes", "Note");
                    }
                }

                _logger.LogWarning("[CommentController] Error new note upload, ModelState invalid");
                return View(comment); //Returns to source
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error comment upload");
                throw;
            }
        }




        [HttpGet]
        [Authorize]
        public async Task<IActionResult> EditCommentNote(int id, string source = "Notes") //Method for editing a comment under a note
        {
            var comment = await _CommentRepository.GetCommentById(id);

            if (comment == null)
            {
                _logger.LogError("[CommentController] Could not find comment with id {Id}", id);
                return NotFound();
            }

            var currentUserName = _userManager.GetUserName(User); //Check for authentication
            if (comment.UserName != currentUserName)
            {
                _logger.LogWarning("Unauthorized edit attempt by user {UserName} for comment {CommentId}", currentUserName, id);
                return Forbid();
            }

            TempData["Source"] = source; //Store source in TempData for later use in view
            return View(comment);
        }


        [HttpPost]
        [Authorize]
        public async Task<IActionResult> EditCommentNote(int id, Comment updatedComment, string source) //Method for editing a comment under a note
        {
            if (id != updatedComment.CommentId || !ModelState.IsValid)
            {
                TempData["Source"] = source; //Preserve source value in case of validation error
                return View(updatedComment);
            }

            var existingComment = await _CommentRepository.GetCommentById(id); //Check if comment exists
            if (existingComment == null)
            {
                _logger.LogError("Could not find comment ID {CommentId}", updatedComment.CommentId);
                return NotFound();
            }

            var currentUserName = _userManager.GetUserName(User); //Check for authentication
            if (existingComment.UserName != currentUserName)
            {
                _logger.LogWarning("Unauthorized edit attempt by user {UserName} for comment {CommentId}", currentUserName, updatedComment.CommentId);
                return Forbid();
            }

            //Update the comment content and timestamp after editing
            existingComment.CommentDescription = updatedComment.CommentDescription;
            existingComment.CommentTime = DateTime.Now;

            bool success = await _CommentRepository.Edit(existingComment);
            if (success)
            {
                //Redirect to the correct page based on the Source parameter
                return RedirectToAction(source == "Notes" ? "Notes" : "MyPage", "Note", new { id = existingComment.NoteId });
            }
            else
            {
                _logger.LogWarning("[CommentController] Could not update the comment.");
                TempData["Source"] = source; //Preserve source value in case of update failure
                return View(updatedComment);
            }
        }


        [HttpGet]
        [Authorize]
        public async Task<IActionResult> DeleteCommentNote(int id, string source = "Notes") //Method for deleting a comment under note
        {
            var comment = await _CommentRepository.GetCommentById(id);//Retrieve the comment using repo

            if (comment == null)
            {
                _logger.LogWarning("Comment not found when trying to delete, comment ID: {CommentId}", id);
                return NotFound();
            }

            var currentUserName = _userManager.GetUserName(User); //Check for authentication
            if (comment.UserName != currentUserName)
            {
                _logger.LogWarning("Unauthorized delete attempt by user {UserName} for comment {CommentId}", currentUserName, id);
                return Forbid();
            }

            TempData["Source"] = source; //Store source in TempData for later use in view
            return View(comment);
        }


        [HttpPost]
        [Authorize]
        public async Task<IActionResult> DeleteConfirmedCommentNote(int id, string source) //Method for finally deleting a comment under note
        {
            var comment = await _CommentRepository.GetCommentById(id); //Retrieve the comment using repo
            if (comment == null)
            {
                _logger.LogWarning("Comment not found when trying to delete, comment ID: {CommentId}", id);
                return NotFound();
            }

            var currentUserName = _userManager.GetUserName(User); //Check for authentication
            if (comment.UserName != currentUserName)
            {
                _logger.LogWarning("Unauthorized delete attempt by user {UserName} for comment {CommentId}", currentUserName, id);
                return Forbid();
            }

            bool success = await _CommentRepository.Delete(id); //Returns True if deletion successful
            if (!success)
            {
                _logger.LogError("Comment with ID {CommentId} was not deleted successfully", id);
                return BadRequest("Comment not deleted");
            }

            _logger.LogInformation("Comment with ID {CommentId} was deleted successfully", id);

            //Redirect to the correct page based on the Source parameter
            return RedirectToAction(source == "Notes" ? "Notes" : "MyPage", "Note");

        }
        


    }
}