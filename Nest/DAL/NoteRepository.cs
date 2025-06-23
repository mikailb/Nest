using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nest.Models;
using Nest.DAL;



namespace Nest.DAL;

public class NoteRepository : INoteRepository
{
    private readonly MediaDbContext _db;
    private readonly ILogger<NoteRepository> _logger;

    public NoteRepository(MediaDbContext db, ILogger<NoteRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IEnumerable<Note>?> GetAll()
    {
        try
        {
            return await _db.Notes.ToListAsync();
        }
        catch (Exception e)
        {
            _logger.LogError("[NoteRepository] Notes ToListAsync() failed when GetAll, error message: {e}", e);
            return null;
        }
    }

    public async Task<Note?> GetNoteById(int NoteId)
    {
        try
        {
            return await _db.Notes.FindAsync(NoteId);
        }
        catch (Exception e)
        {
            _logger.LogError("[NoteRepository] note FirstOrDefault() failed when GetNoteById for NoteId {NoteId}, error message: {e}", NoteId, e);
            return null;
        }
    }

    public async Task Create(Note note)
    {
        try
        {
            await _db.Notes.AddAsync(note);
            await _db.SaveChangesAsync();
        }
        catch (Exception e)
        {
            _logger.LogError("[NoteRepository] note creation failed for note {@note}, error message: {e}", note, e.Message);
        }
    }

    public async Task Edit(Note note)
    {
        try
        {
            _db.Notes.Update(note);
            await _db.SaveChangesAsync();
        }
        catch (Exception e)
        {
            _logger.LogError("[NoteRepository] note update failed for note {@note}, error message: {e}", note, e.Message);
        }
    }

    public async Task<bool> Delete(int NoteId)
    {
        var note = await _db.Notes.FindAsync(NoteId); 
        if (note != null)
        {
            _db.Notes.Remove(note);  
            await _db.SaveChangesAsync();
            return true;
        }
        else
        {
            _logger.LogError("Failed finding note with id: {NoteId}", NoteId);
            return false;
        }
    }

    public async Task<bool> DeleteConfirmed(int NoteId)
    {
        var note = await _db.Notes.FindAsync(NoteId); 
        if (note != null)
        {
            _db.Notes.Remove(note); 
            await _db.SaveChangesAsync();  
            return true;
        }
        return false;
    }
}
