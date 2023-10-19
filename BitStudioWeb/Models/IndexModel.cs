using System.Collections.Generic;
using System.Linq;

namespace BitStudioWeb.Models
{
    public class IndexModel
    {
        public IndexModel(List<ReleaseGroup> g)
        {
            Relese = g;
        }
        public List<ReleaseGroup> Relese
        {
            get;
           private set;
        }

        public ReleaseModel this[string os]
        {
            get 
            {
                 
                var query = from c in Relese
                            where c.os == os
                            select c.releaseModel;
                var r= query.FirstOrDefault();
                if (r == null)
                    r = new ReleaseModel();
                return r;
            }
        }
         
    }
}
