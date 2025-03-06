using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebCrawler2
{
    public class PageData
    {
        public string FileName { get; set; }
        public string Content { get; set; }
        public string Url { get; set; }
        public string Title { get; set; }
        public List<string> Headings { get; set; } = new List<string>();
    }
}