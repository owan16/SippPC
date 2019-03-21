using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sipp_PC
{
    class ScratchData
    {
        
        private string title;
        private string coverUrl;
        private string description;
        private string scratchUrl;
        private string docUrl;

        internal ScratchData(string name, string coverUrl, string description, string scratchUrl, string docUrl) {
            
            this.title = name;
            this.coverUrl = coverUrl;
            this.description = description;
            this.scratchUrl = scratchUrl;
            this.docUrl = docUrl;
        }

        public string CoverUrl
        {
            get
            {
                return coverUrl;
            }
        }
        public string Title
        {
            get {
                return title;
            }
        }
        public string ScratchUrl
        {
            get {
                return scratchUrl;
            }
        }
        public string Description
        {
            get {
                return description;
            }
        }
    }
}
