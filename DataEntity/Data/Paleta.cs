using PropertyChanged;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataEntity
{
    [AddINotifyPropertyChangedInterface]
    [Table("Palety")]
    public class Paleta : BaseModel
    {
        [Key]
        public int PaletaId { get; set; }

        [Required(ErrorMessage = "Typ palety je povinné pole")]
        public Enums.PaletaTyp Typ { get; set; }

        [Required(ErrorMessage = "Stav palety je povinné pole")]
        public Enums.PaletaStav Stav { get; set; }

        [StringLength(10, ErrorMessage = "Maximální délka adresy je 10 znaků")]
        public string? AdresaUlozeni { get; set; }

        [Required(ErrorMessage = "Množství je povinné pole")]
        [Range(0, int.MaxValue, ErrorMessage = "Množství musí být nezáporné číslo")]
        public int Mnozstvi { get; set; } = 0;

        // Foreign Key for Material
        public int? MaterialId { get; set; }

        [ForeignKey(nameof(MaterialId))]
        public virtual Material? Material { get; set; }
    }
}
