
using Nest.Models;
using Microsoft.EntityFrameworkCore;

namespace Nest.DAL
{
    public class CommentRepository : ICommentRepository
    {
        private readonly MediaDbContext _context;

        private readonly ILogger<CommentRepository> _logger;

        public CommentRepository(MediaDbContext context, ILogger<CommentRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<Comment>> GetAll()
        {
            try
            {
                return await _context.Comments.ToListAsync();

            }
            catch (Exception e)
            {
                _logger.LogError(e, "[CommentRepository] Comment ToListAsync failed using GetAll(). Error: {ErrorMessage}", e.Message);
                return Enumerable.Empty<Comment>();
            }
        }


        public async Task<Comment?> GetCommentById(int id)
        {
            try
            {
                return await _context.Comments.FindAsync(id);

            }
            catch (Exception e)
            {
                _logger.LogError(e, "[CommentRepository] Failed using GetCommentById for CommentId {CommentId:0000}. Error: {ErrorMessage}", id, e.Message);
                return null;

            }
        }

        public async Task<int?> GetPictureId(int id)
        {
            var comment = await _context.Comments.FindAsync(id);
            if (comment == null)
            {
                _logger.LogWarning("Comment with ID {CommentId} not found when getting PictureId", id);
                return null;
            }
            return comment.PictureId;
        }
        public async Task<int?> GetNoteId(int id)
        {
            var comment = await _context.Comments.FindAsync(id);
            if (comment == null)
            {
                _logger.LogWarning("Comment with ID {CommentId} not found when getting NoteId", id);
                return null;
            }
            return comment.NoteId;
        }

        public async Task Create(Comment comment)
        {
            try
            {
                _context.Comments.Add(comment);
                await _context.SaveChangesAsync();

            }
            catch (Exception e)
            {
                _logger.LogError(e, "[CommentRepository] Error comment upload with id {@Comment}. error: {ErrorMessage}", comment, e.Message);
            }
        }

        public async Task<bool> Edit(Comment comment)
        {
            try
            {
                _context.Comments.Update(comment);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError("[CommentRepository] Error updating comment ID {CommentId}, error: {e}", comment.CommentId, e.Message);
                return false;
            }
        }


        public async Task<bool> Delete(int id)
        {
            try
            {
                var comment = await _context.Comments.FindAsync(id);

                if (comment == null)
                {
                    return false;
                }
                _context.Comments.Remove(comment);
                await _context.SaveChangesAsync();
                return true;
            }


            catch (Exception e)
            {
                _logger.LogError(e, "[CommentRepository] failed deleting comment with ID {CommentId:0000}. error: {ErrorMessage}", id, e.Message);
                return false;
            }

        }

    }
}