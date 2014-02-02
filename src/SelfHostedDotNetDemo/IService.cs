using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Web;
using SelfHostedJSONExample.Models;

namespace SelfHostedJSONExample
{
    [ServiceContract]
    public interface IService
    {
        [OperationContract, WebGet(UriTemplate = "/people", ResponseFormat = WebMessageFormat.Json)]
        List<Person> GetPeople();

        [OperationContract, WebGet(UriTemplate = "/person/{id}", ResponseFormat = WebMessageFormat.Json)]
        Person GetPerson(string id);
    }
}
