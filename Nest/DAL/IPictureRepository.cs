using System.Collections.Generic;
using System.Threading.Tasks;
using Nest.Models;

namespace Nest.DAL
{
    public interface IPictureRepository
    {
        
        Task<IEnumerable<Picture>?> GetAll();

        
        Task<bool> Create(Picture picture);

      
        Task<Picture?> PictureId(int id);

        
        Task<bool> Edit(Picture picture);

        
        Task<bool> Delete(int id);



    }


}