using System;
using System.ComponentModel.DataAnnotations;

namespace Nest.Models
{
    public class Comment
    {
        //Primary Key
        public int CommentId { get; set; }

        //Foreign Key to Picture
        public int? PictureId { get; set; } // Optional, since it's nullable

        //Foreign Key to Note
        public int? NoteId { get; set; } // Optional, since it's nullable

        //Comment content/description
        [Required(ErrorMessage = "Comment description is required.")]
        [StringLength(500, ErrorMessage = "Comment description cannot exceed 500 characters.")]
        public string? CommentDescription { get; set; }

        //Timestamp for when the comment was made
        [Required]
        public DateTime CommentTime { get; set; }

        //Navigation property for related Picture
        public virtual Picture? Picture { get; set; }

        //Navigation property for related Note
        public virtual Note? Note { get; set; }

        //Username associated with the comment
        [StringLength(100, ErrorMessage = "User name cannot exceed 100 characters.")]
        public string? UserName { get; set; }

        
    }
}
