using System;

namespace WindowsFormsDemo
{
    // in this class you can save any type of data and restore it
    public class MyState
    {
        public string Text { get; set; }

        public DateTime DateSaved { get; set; }
    }
}