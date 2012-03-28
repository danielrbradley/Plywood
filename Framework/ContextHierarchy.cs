using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Plywood
{
    public class ContextHierarchy
    {
        private string hierarchy;

        public ContextHierarchy()
        {
        }

        public ContextHierarchy(string hierarchy)
        {
            this.hierarchy = hierarchy;
        }

        public string FullName
        {
            get
            {
                return hierarchy;
            }
            set
            {
                this.hierarchy = value;
            }
        }

        public string Name
        {
            get
            {
                return ContextHierarchy.GetName(this.hierarchy);
            }
        }

        public ContextHierarchy Parent
        {
            get
            {
                var context = this.Context;
                if (context == null)
                    return null;
                else
                    return new ContextHierarchy(context);
            }
        }

        public string Context
        {
            get
            {
                return ContextHierarchy.GetContext(this.hierarchy);
            }
        }

        public bool IsValid
        {
            get
            {
                return ContextHierarchy.IsHierarchyValid(this.hierarchy);
            }
        }

        public override string ToString()
        {
            return this.hierarchy;
        }

        public static string GetName(string hierarchy)
        {
            if (hierarchy == null)
                throw new ArgumentNullException("hierarchy", "hierarchy is null.");

            var index = hierarchy.LastIndexOf('.');
            if (index < 0)
                return hierarchy;
            else if (index < hierarchy.Length)
                return hierarchy.Substring(index + 1);
            else
                return string.Empty;
        }

        public static string GetContext(string hierarchy)
        {
            if (hierarchy == null)
                throw new ArgumentNullException("hierarchy", "hierarchy is null.");

            var index = hierarchy.LastIndexOf('.');
            if (index < 0)
                return null;
            else
                return hierarchy.Substring(0, index);
        }

        public static bool IsHierarchyValid(string hierarchy)
        {
            return Regex.IsMatch(hierarchy, @"^(?:\w+\.)*\w+$");
        }
    }
}
