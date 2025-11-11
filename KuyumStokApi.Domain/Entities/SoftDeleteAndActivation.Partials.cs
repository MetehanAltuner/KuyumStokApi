using KuyumStokApi.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace KuyumStokApi.Domain.Entities
{
    // Katalog
    public partial class ProductCategories : ISoftDeletable, IActivatable { }
    public partial class ProductTypes : ISoftDeletable, IActivatable { }
    public partial class ProductVariants : ISoftDeletable, IActivatable { }

    // Organizasyon
    public partial class Stores : ISoftDeletable, IActivatable { }
    public partial class Branches : ISoftDeletable, IActivatable { }
    public partial class Roles : ISoftDeletable, IActivatable { }

    // Stamper
    public partial class Banks : ISoftDeletable, IActivatable { }
    public partial class Customers : ISoftDeletable, IActivatable { }
    public partial class PaymentMethods : ISoftDeletable, IActivatable { }

    // Users
    public partial class Users : ISoftDeletable { }
    public partial class ThermalPrinters : ISoftDeletable, IActivatable { }
}
