using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Client.Data
{
    public class ControllerData
    {

        private string _id;
        [Key]
        public string Id { get { return _id; } set { _id = value; } }
        //гуид контроллера который отправил показания
        public string Guid { get; set; }

        public long Value { get; set; }

        public DateTime Time { get;  set; }


    }


}