using Microsoft.AspNetCore.Mvc;
using Nest.Models;
using Nest.DAL;
using Nest.ViewModels;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Nest.Utilities;

namespace Nest.Controllers
{
    public class PictureController : Controller
    {
        private readonly IPictureRepository _pictureRepository;
        private readonly ICommentRepository _commentRepository;
        private readonly ILogger<PictureController> _logger;
        private readonly UserManager<IdentityUser> _userManager;

        public PictureController(IPictureRepository pictureRepository, ICommentRepository commentRepository, ILogger<PictureController> logger, UserManager<IdentityUser> userManager)
        {
            _commentRepository = commentRepository;
            _pictureRepository = pictureRepository;
            _logger = logger;
            _userManager = userManager;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> MyPage()  //Showing users pictures
        {
            var currentUserName = _userManager.GetUserName(User); //Check for authentication
            if (string.IsNullOrEmpty(currentUserName))
            {
                _logger.LogError("[PictureController] Current user is null or empty when accessing MyPage.");
                return Unauthorized();
            }

            var allPictures = await _pictureRepository.GetAll(); //Retrieve pictures based on username
            if (allPictures == null)
            {
                _logger.LogError("[PictureController] Could not retrieve images for user {UserName}", currentUserName);
                allPictures = Enumerable.Empty<Picture>();
            }

            var userPictures = allPictures.Where(b => b.UserName == currentUserName).ToList(); //filters for user

            var pictureViewModel = new PicturesViewModel(userPictures, "MyPage"); //Uses ViewModel 

            ViewData["IsMyPage"] = true; 
            return View("MyPage", pictureViewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Picture()
        {
            var pictures = await _pictureRepository.GetAll() ?? Enumerable.Empty<Picture>();
            var pictureViewModel = new PicturesViewModel(pictures, "Picture");

            if (pictures == null)
            {
                _logger.LogError("[PictureController] Picture list, not found.");
            }

            return View(pictureViewModel);
        }

        public async Task<IActionResult> Grid() //Feed for pictures
        {
            var pictures = await _pictureRepository.GetAll() ?? Enumerable.Empty<Picture>(); //Retrieve all pictures in database
            var pictureViewModel = new PicturesViewModel(pictures, "Picture");

            if (pictures == null) //If no pictures are in the database
            {
                _logger.LogError("[PictureController] Picture list, not found.");
                return NotFound("Pictures not found");
            }

            ViewData["IsMyPage"] = false; 
            return View(pictureViewModel);
        }

        [HttpGet]
        [Authorize]
        public IActionResult Create() //Method for creating a picture
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create(Picture newImage, IFormFile PictureUrl) //Method for creating a picture
        {
            var time = DateTime.Now;
            newImage.UploadDate = time; //Sets the time and date it was created

            if (!ModelState.IsValid) //Has to be a valid model
            {
                return View(newImage);
            }

            var UserName = _userManager.GetUserName(User); //Check for authentication
            newImage.UserName = UserName;

            if (PictureUrl != null && PictureUrl.Length > 0)
            {
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images"); //Choosing path which is wwwroot
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder); //If image directory does not exist, it gets created
                }

                string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(PictureUrl.FileName);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName); //Make sure the filename is unique 

                using (var fileStream = new FileStream(filePath, FileMode.Create)) //downloads the file
                {
                    await PictureUrl.CopyToAsync(fileStream);
                }

                newImage.PictureUrl = "/images/" + uniqueFileName; //Finally saves
            }

            bool success = await _pictureRepository.Create(newImage); //Returns true if created successfully
            if (success)
            {
                return RedirectToAction(nameof(MyPage));
            }
            else
            {
                _logger.LogWarning("[PictureController] Could not create new image.");
                return View(newImage);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id, string source = "Grid") //Method to redirect to specified picture post
        {
            var picture = await _pictureRepository.PictureId(id); //finds the picture by its id
            if (picture == null)
            {
                _logger.LogError("[PictureController] picture id not found");
                return NotFound();
            }

            ViewBag.Source = source; 
            return View("PictureDetails", picture);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Edit(int id, string source = "Grid") //Method for editing a pictures description and title
        {
            var picture = await _pictureRepository.PictureId(id); //finds chosen picture
            if (picture == null)
            {
                _logger.LogError("The image with id {PictureId} was not found", id);
                return NotFound();
            }

            var currentUserName = _userManager.GetUserName(User); //Check for authentication
            if (picture.UserName != currentUserName)
            {
                _logger.LogWarning("Unauthorized edit attempt by user {UserId} for image {PictureId}", currentUserName, id);
                return Forbid();
            }

            TempData["Source"] = source; //Store source in TempData for later use in view
            return View(picture);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Edit(int id, Picture updatedPicture, IFormFile? newPictureUrl, string source) //Method for editing a pictures description and title
        {
            if (id != updatedPicture.PictureId || !ModelState.IsValid) //Has to be a valid model
            {
                TempData["Source"] = source; //Preserve source value in case of validation error
                return View(updatedPicture);
            }

            var existingPicture = await _pictureRepository.PictureId(id);
            if (existingPicture == null)
            {
                _logger.LogError("The image with id {PictureId} was not found", id);
                return NotFound();
            }

            var currentUserName = _userManager.GetUserName(User); //Check for authentication
            if (existingPicture.UserName != currentUserName)
            {
                _logger.LogWarning("Unauthorized edit attempt by user {UserName} for image {PictureId}", currentUserName, id);
                return Forbid();
            }

            //Update title and description
            existingPicture.Title = updatedPicture.Title;
            existingPicture.Description = updatedPicture.Description;

            //Update the image if a new one is uploaded
            if (newPictureUrl != null && newPictureUrl.Length > 0)
            {
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(newPictureUrl.FileName);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await newPictureUrl.CopyToAsync(fileStream);
                }

                //Delete the old image
                if (!string.IsNullOrEmpty(existingPicture.PictureUrl))
                {
                    string oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", existingPicture.PictureUrl.TrimStart('/'));
                    if (FileUtil.FileExists(oldFilePath))
                    {
                        FileUtil.FileDelete(oldFilePath);
                    }
                }

                existingPicture.PictureUrl = "/images/" + uniqueFileName;
            }

            bool success = await _pictureRepository.Edit(existingPicture);
            if (success)
            {
                //Redirect to the correct page 
                return RedirectToAction(source == "MyPage" ? "MyPage" : "Grid");
            }
            else
            {
                _logger.LogWarning("[PictureController] Could not update the image.");
                TempData["Source"] = source; //Store source in TempData for later use in view
                return View(updatedPicture);
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Delete(int id, string source = "Grid") //Method for deleting a picture post
        {
            var picture = await _pictureRepository.PictureId(id); //Finds which picture to delete
            if (picture == null)
            {
                _logger.LogError("[PictureController] picture with Id not found {id}", id);
                return NotFound();
            }

            var currentUserName = _userManager.GetUserName(User); //Check for authentication
            if (picture.UserName != currentUserName)
            {
                _logger.LogWarning("Unauthorized delete attempt by user {UserName} for image {PictureId}", currentUserName, id);
                return Forbid();
            }

            TempData["Source"] = source; //Store source in TempData for later use in view
            return View(picture);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> DeleteConfirmed(int id, string source) //Method for deleting a picture post
        {
            var picture = await _pictureRepository.PictureId(id); //Finds which picture to delete
            if (picture == null)
            {
                _logger.LogError("[PictureController] picture with Id not found {id}", id);
                return NotFound();
            }

            var currentUserName = _userManager.GetUserName(User); //Check for authentication
            if (picture.UserName != currentUserName)
            {
                _logger.LogWarning("Unauthorized delete attempt by user {UserName} for image {PictureId}", currentUserName, id);
                return Forbid();
            }

            if (!string.IsNullOrEmpty(picture.PictureUrl))
            {
                string fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", picture.PictureUrl.TrimStart('/'));

                if (FileUtil.FileExists(fullPath)) //Checking for filepath and deletes if found
                {
                    FileUtil.FileDelete(fullPath);
                }
            }

            bool success = await _pictureRepository.Delete(id); //Returns true if deleted successfully
            if (!success)
            {
                _logger.LogError("[PictureController] picture not deleted with {Id}", id);
                return BadRequest("Picture not deleted");
            }

            //Redirect to the correct page 
            return RedirectToAction(source == "MyPage" ? "MyPage" : "Grid");
        }

        public IActionResult Home()
        {
            //Path to the images folder in wwwroot
            var imageFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images-carousel");
        
            //Get all image file paths
            var images = Directory.GetFiles(imageFolderPath)
                              .Select(file => "/images-carousel/" + Path.GetFileName(file))
                              .ToList();

            return View(images); 
        }
    }
}
