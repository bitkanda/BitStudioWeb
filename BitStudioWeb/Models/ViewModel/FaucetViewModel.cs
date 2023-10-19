using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace NethereumFaucet.ViewModel
{
    public class FaucetViewModel
    {
        [Required]
        [StringLength(42, ErrorMessage = "Addresses should be (40 - 42) characters in length", MinimumLength = 40)]
        public string Address { get; set; }

        public string TransactionHash { get; set; }

        public decimal Amount { get; set; }

        public string SourceAddress { get; set; }
    }
}
