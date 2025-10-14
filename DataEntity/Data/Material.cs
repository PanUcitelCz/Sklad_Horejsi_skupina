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
    [Table("Materialy")]
    public class Material : BaseModel
    {

        [Key]
        public int MaterialId { get; set; }

        [Required(ErrorMessage = "Název materiálu je povinné pole")]
        [StringLength(150, ErrorMessage = "Maximální délka je 150")]
        public string Nazev {  get; set; }

        [Required(ErrorMessage = "Pojistné množství je povinné pole")]
        [Range (0, int.MaxValue, ErrorMessage = "Pojistné množství musí být kladné")]
        public int MnozPoj { get; set; } = 0;

        [Required(ErrorMessage = "Množství do palety je povinné pole")]
        [Range(0, int.MaxValue, ErrorMessage = "Množství do palety musí být kladné")]
        public int MnozDoPal { get; set; } = 0;

        [Column (TypeName = "date")]
        public DateTime Datum { get; set; } = DateTime.Now;

        public string? Komentar { get; set; }

        public int MernaJednotkaId { get; set; }

        [ForeignKey (nameof(MernaJednotkaId))]
        public virtual MernaJednotka MernaJednotka { get; set; }

        public virtual ObservableCollection<Paleta> Palety { get; set; }

    }
}
