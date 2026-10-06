using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ticksi.Application.DTOs
{
    public sealed class SearchSuggestionDto
    {

        public Guid PublicId { get; set; }          // <-- BITNO: Guid, ne string
        public string Type { get; set; } = "";      // "event" | "location" | "category"
        public string Label { get; set; } = "";     // šta prikazuješ u UI
        public double Score { get; set; }              // za ranking
    }
}
