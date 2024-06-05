using System;
using System.Collections.Generic;
using System.Text;

namespace Landis.Extension.Succession.ForC
{
    public interface ITimeInput
    {
        int Year { get; }
    }

    public interface ITimeCollection<T> where T:ITimeInput
    {
        /// <summary>
        /// Returns true if there is an ITimeInput in the collection with the same Year, false otherwise.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        bool Contains(T value);

        /// <summary>
        /// Adds an ITimeInput object to the collection.  If there is already an object
        /// for the given ITimeInput.Year in the collection, the existing object is
        /// removed and replaced by this one.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        void Add(T value);

        /// <summary>
        /// Returns True with the given ITimeInput object if found a 'matching' ITimeInput object in the collection.
        /// </summary>
        /// <param name="nYear"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        bool TryGetValue(int nYear, out T value);
    }

    public class TimeInput : ITimeInput
    {
        int m_nYear = 0;

        /// <summary>
        /// Default ctor
        /// </summary>
        public TimeInput()
        {
        }

        public TimeInput(int nYear)
        {
            this.Year = nYear;
        }

        public int Year
        {
            get
            {
                return m_nYear;
            }
            set
            {
                //if (value < 0)
                //    throw new Edu.Wisc.Forest.Flel.Util.InputValueException(value.ToString(), "Year must be >= 0.  The value provided is = {0}.", value);
                m_nYear = value;
            }
        }
    }

    public class TimeCollection<T> : ITimeCollection<T> where T:ITimeInput
    {
        protected System.Collections.Generic.SortedList<int, T> m_listValues = new System.Collections.Generic.SortedList<int, T>();
        
        /// <summary>
        /// Returns true if there is an ITimeInput (or derived) in the collection with the same Year, false otherwise.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool Contains(T value)
        {
            return m_listValues.ContainsKey(value.Year);
        }
        
        /// <summary>
        /// Adds an ITimeInput object (or derived) to the collection.  If there is already an object
        /// for the given ITimeInput.Year in the collection, the existing object is
        /// removed and replaced by this one.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public void Add(T value)
        {
            if (this.Contains(value))
                m_listValues.Remove(value.Year);

            m_listValues.Add(value.Year, value);
        }

        /// <summary>
        /// Tries to get the matching ITimeInput (or derived) object for the given year.
        /// </summary>
        /// <param name="nYear"></param>
        /// <param name="value">When True is returned, value is set to the ITimeInput (or derived) object.</param>
        /// <returns>True - If there is an ITimeInput (or derived) object that 'matches' the given year.
        /// False otherwise.</returns>
        /// <remarks>Note that a match is the object which has the same or closest Year value.</remarks>
        public bool TryGetValue(int nYear, out T value)
        {
            // default Keywoard in Generic Code (C# Programming Guide)
            // http://msdn.microsoft.com/en-us/library/xwth0h0d(VS.80).aspx
            // How can I return NULL from a generic method in C#?
            // http://stackoverflow.com/questions/302096/how-can-i-return-null-from-a-generic-method-in-c
            value = default(T); // null
            // Iterate the list in reverse order, returning the ITimeInput object with a Year <= value.Year
            for (int n = (m_listValues.Count - 1); n >= 0; n--)
            {
                if (m_listValues.Values[n].Year <= nYear)
                {
                    value = m_listValues.Values[n];
                    return true;
                }
            }
            return false;
        }
    }
}
