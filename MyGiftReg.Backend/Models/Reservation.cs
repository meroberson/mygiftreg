using System.Collections.ObjectModel;
using Microsoft.Identity.Client.Advanced;

namespace MyGiftReg.Backend.Models
{
    public class Reservation
    {
        public string UserId { get; set; } = string.Empty;
        public string UserDisplayName { get; set; } = string.Empty;
        public int Quantity { get; set; } = 1;

        // override object.Equals
        public override bool Equals(object? obj)
        {
            //
            // See the full list of guidelines at
            //   http://go.microsoft.com/fwlink/?LinkID=85237
            // and also the guidance for operator== at
            //   http://go.microsoft.com/fwlink/?LinkId=85238
            //
            
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            Reservation r = obj as Reservation ?? new Reservation();
            
            return UserId == r.UserId && UserDisplayName == r.UserDisplayName && Quantity == r.Quantity;
        }
        
        // override object.GetHashCode
        public override int GetHashCode()
        {
            return UserId.GetHashCode() + UserDisplayName.GetHashCode() + Quantity.GetHashCode();
        }
    }
}
