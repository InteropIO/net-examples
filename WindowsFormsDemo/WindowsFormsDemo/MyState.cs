using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsDemo
{
    // in this class you can save any type of data and restore it
    public class MyState
    {
        public string Text { get; set; }

        public DateTime DateSaved { get; set; }
    }
}
