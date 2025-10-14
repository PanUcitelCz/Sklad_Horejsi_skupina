using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataEntity
{
    [AddINotifyPropertyChangedInterface()]
    [Table ("MerneJednotky")]
    public class MernaJednotka : BaseModel
    {
        [Key]
        public int MernaJednotkaId { get; set; }

        [StringLength(50, ErrorMessage = "Popis maximálně na 50 znaků.")]
        public string Popis {  get; set; }

        public virtual ObservableCollection<Material> Materialy { get; set; }

    }
}
