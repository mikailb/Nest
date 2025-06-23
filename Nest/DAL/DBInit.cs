using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Nest.Models;
using Nest.DAL;

namespace Nest.DAL;

public static class DBInit
{
    
    public static async Task SeedAsync(IApplicationBuilder app)
    {
        
        using var serviceScope = app.ApplicationServices.CreateScope();

        
        var context = serviceScope.ServiceProvider.GetRequiredService<MediaDbContext>();

        
        var userManager = serviceScope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        
        var defaultUser = await userManager.FindByEmailAsync("Ali123@hotmail.com");
        if (defaultUser == null)
        {
            
            defaultUser = new IdentityUser
            {
                UserName = "Ali123@hotmail.com",
                Email = "Ali123@hotmail.com",
                EmailConfirmed = true
            };
            
            await userManager.CreateAsync(defaultUser, "Bekam2305."); 
        }

      
        if (!context.Pictures.Any())
        {
            var pictures = new List<Picture>
            {
                new Picture
                {
                    Title = "Digg",
                    Description = "Va.",
                    PictureUrl = "/images/Solnedgang_JPG.jpg",
                    UploadDate = DateTime.Now.AddDays(-10),
                    UserName = defaultUser.UserName
                }
            };
            
            context.Pictures.AddRange(pictures);
            await context.SaveChangesAsync(); 
        }

     
        if (!context.Notes.Any())
        {
            var notes = new List<Note>
            {
                new Note
                {
                    Title = "Dagbok - Dag 1",
                    Content = "Startet dagen med en god frokost og dro på tur.",
                    UploadDate = DateTime.Now.AddDays(-10),
                    username = defaultUser.UserName
                },
                new Note
                {
                    Title = "Dagbok - Dag 2",
                    Content = "Møtte noen venner for fjelltur. Fantastisk utsikt!",
                    UploadDate = DateTime.Now.AddDays(-9),
                    username = defaultUser.UserName
                }
            };

          
            context.Notes.AddRange(notes);
            await context.SaveChangesAsync(); 
        }

        
        if (!context.Comments.Any())
        {
            var comments = new List<Comment>
            {
                new Comment { PictureId = 1, CommentDescription = "Amazing picture!", CommentTime = DateTime.Now.AddDays(-9) }, // Comment on the image
                new Comment { PictureId = 1, CommentDescription = "Sunset is magical!", CommentTime = DateTime.Now.AddDays(-8) } // Comment on the image
            };
            
            context.Comments.AddRange(comments);
            await context.SaveChangesAsync(); 
        }

        
        if (!context.Comments.Any(k => k.NoteId == 1))
        {
            var noteComments = new List<Comment>
            {
                new Comment { NoteId = 1, CommentDescription = "Great start to the day!", CommentTime = DateTime.Now.AddDays(-5) },
                new Comment { NoteId = 1, CommentDescription = "Sounds amazing!", CommentTime = DateTime.Now.AddDays(-4) }
            };

           
            context.Comments.AddRange(noteComments);
            await context.SaveChangesAsync(); 
        }
    }
}
