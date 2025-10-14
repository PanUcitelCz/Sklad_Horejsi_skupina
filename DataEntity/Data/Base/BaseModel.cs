using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DataEntity
{
    public abstract class BaseModel : IDataErrorInfo
    {

        #region "validace"

        string IDataErrorInfo.Error
        {
            get
            {
                //return null;
                throw new NotSupportedException("IDataErrorInfo.Error is not supported, use IDataErrorInfo.this[propertyName] instead.");
            }
        }

        string IDataErrorInfo.this[string propertyName]
        {
            get
            {
                // return null;
                return OnValidate(propertyName);
            }
        }



        protected virtual string OnValidate(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
                throw new ArgumentException("Invalid property name", propertyName);

            string error = string.Empty;
            var value = this.GetType().GetProperty(propertyName).GetValue(this, null);
            var results = new List<ValidationResult>(1);

            var context = new ValidationContext(this, null, null) { MemberName = propertyName };

            var result = Validator.TryValidateProperty(value, context, results);

            if (!result)
            {
                var validationResult = results.First();
                error = validationResult.ErrorMessage;
            }

            return error;
        }

        #endregion

        [Timestamp]
        public byte[] RowVersion { get; set; }

        public DateTime DatumVytvoreni { get; set; } = DateTime.Now;

    }
}
