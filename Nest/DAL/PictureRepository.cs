using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Nest.Models;
using Nest.DAL;
using Microsoft.Extensions.Logging;


namespace Nest.DAL
{
    public class PictureRepository : IPictureRepository
    {
        private readonly MediaDbContext _context;
        private readonly ILogger<PictureRepository> _logger;

        public PictureRepository(MediaDbContext context, ILogger<PictureRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<Picture>?> GetAll()
        {
            try
            {
                return await _context.Pictures.ToListAsync();
            }
            catch (Exception e)
            {
                _logger.LogError("[PictureRepository] index failed getting all pictures. error: {e}", e.Message);
                return null;
            }
        }

        public async Task<bool> Create(Picture picture)
        {
            try
            {
                await _context.Pictures.AddAsync(picture);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Picture uploaded with ID: {0}", picture.PictureId);
                return true;
            }
            catch(Exception e)
            {
                _logger.LogError("[PictureRepository] uploading picture {@picture} failed, error: {e} ", picture , e.Message);
                return false;
            }
        }

        public async Task<Picture?> PictureId(int id)
        {
            try
            {
                return await _context.Pictures.FindAsync(id);
            }
            catch (Exception e)
            {
                _logger.LogError("[PictureRepository] Getting picture ID failed  {id} (FindAsync), error: {e}", id, e.Message);
                return null;
            }
        }

        public async Task<bool> Edit(Picture picture)
        {
           try{
            _context.Pictures.Update(picture);
            await _context.SaveChangesAsync();
            return true;
           }
           catch(Exception e)
           {
            _logger.LogError("[PictureRepository] Picture update failed ID {PictureId} , error: {e}", picture.PictureId,e.Message);
            return false;
           }
        }

        public async Task<bool> Delete(int id)
        {
            try
            {
                var picture = await _context.Pictures.FindAsync(id);
                if (picture == null)
                {
                    _logger.LogError("[PictureRepository] Picture delete failed. Picture with ID {id} not found",id);
                    return false;
                }
                _context.Pictures.Remove(picture);
                await _context.SaveChangesAsync();
                return true;

            }
            catch(Exception e)
            {
                _logger.LogError("[PictureRepository] Picture delete failed with ID {id} , error: {e}", id, e.Message);
                return false;

            }
        }


    }
}