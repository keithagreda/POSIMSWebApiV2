using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POSIMSWebApi.Application.Dtos.EntityHistory
{
    public class EntityHistoryDto
    {
        public string EntityName { get; set; }
        public string Changes { get; set; }
        public string ChangeTime { get; set; }
        public string ChangedBy { get; set; }
        public string Action { get; set; }
    }
}
