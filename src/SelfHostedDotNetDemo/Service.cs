using System.Collections.Generic;
using System.Linq;
using SelfHostedJSONExample.Models;

namespace SelfHostedJSONExample
{
    public class Service : IService
    {
        public List<Person> GetPeople()
        {
            return People;
        }

        public Person GetPerson(string strId)
        {
            int id;
            int.TryParse(strId, out id);
            return People.Where(a => a.Id == id).SingleOrDefault();
        }

        // demo data store
        private List<Person> People
        {
            get
            {
                return new List<Person> 
                {
                    new Person { GivenName = "Stephen", FamilyName = "McMahon", Id = 1, Email = "stephen.mcmahon@host.com" },
                    new Person { GivenName = "Jane", FamilyName = "Private", Id = 2, Email = "jane.private@host.com" },
                    new Person { GivenName = "John", FamilyName = "Public", Id = 3, Email = "jone.public@host.com" }
                };
            }
        }
    }
}
