using Nest.Models;

namespace Nest.ViewModels
{
    public class PicturesViewModel
    {
        public IEnumerable<Picture> Pictures;
        public string? CurrentViewName;

        public PicturesViewModel(IEnumerable<Picture> pictures, string? currentViewName)
        {
            Pictures = pictures;
            CurrentViewName = currentViewName;
        }
    }
}