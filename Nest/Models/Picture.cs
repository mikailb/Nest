using System;
using System.ComponentModel.DataAnnotations;
using Nest.Models;
namespace Nest.Models {
    public class Picture
    {
        //Primary Key
        public int PictureId { get; set; }

        //URL of the Picture
        public string? PictureUrl { get; set; }  // Stores the file path to the picture

        //Title of the Picture
        [Required(ErrorMessage = "Title is required.")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters.")]
        public string? Title { get; set; } // Text for picture title

        //Description for the Picture, maximum of 500 characters
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string? Description { get; set; }

        //Date the Picture was uploaded
        [Required]
        public DateTime UploadDate { get; set; } = DateTime.Now;

        //List of Comments associated with the Picture
        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();

        //Username of the person who uploaded the Picture
        [StringLength(100, ErrorMessage = "Username cannot exceed 100 characters.")]
        public string? UserName { get; set; }
    }

}