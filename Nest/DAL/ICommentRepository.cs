using Nest.Models;


namespace Nest.DAL
{
    public interface ICommentRepository
    {
        Task<IEnumerable<Comment>> GetAll();
        Task<Comment?> GetCommentById(int id);

        Task<int?> GetPictureId(int id);
        Task<int?> GetNoteId(int id);

        Task Create(Comment comment);
        Task<bool> Edit(Comment comment);
        Task<bool> Delete(int id);

    }
}