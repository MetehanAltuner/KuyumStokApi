using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KuyumStokApi.Domain.Common
{
    public interface ISoftDeletable
    {
        bool IsDeleted { get; set; }
        DateTime? DeletedAt { get; set; }      
        int? DeletedBy { get; set; }             
    }

    public interface IActivatable
    {
        bool IsActive { get; set; }
    }
}
