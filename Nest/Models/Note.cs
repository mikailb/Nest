using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Nest.Models
{
    public class Note
    {
        
        public int NoteId { get; set; }

        //Title of the Note
        [Required(ErrorMessage = "Title is required.")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters.")]
        public string Title { get; set; } = string.Empty;

        //Content of the Note
        [Required(ErrorMessage = "Content is required.")]
        [StringLength(2000, ErrorMessage = "Content cannot exceed 2000 characters.")]
        public string Content { get; set; } = string.Empty;

        //Date the Note was uploaded
        [Required]
        public DateTime UploadDate { get; set; } = DateTime.Now;

        //List of Comments associated with the Note
        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();

        //Username of the person who created the Note
        [StringLength(100, ErrorMessage = "Username cannot exceed 100 characters.")]
        public string? username { get; set; }
    }
}
