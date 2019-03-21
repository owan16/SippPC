using Google.Apis.Firestore.v1beta1;
using Google.Apis.Firestore.v1beta1.Data;
using Google.Apis.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Sipp_PC
{
   
    internal class Scratch
    {

        public IList<ScratchData> ScratchProjects
        {
            get
            {
                return scratchProjects;
            }
        }

        BackgroundWorker scratchWorker = new BackgroundWorker();

        static IList<ScratchData> scratchProjects;

        public class DataObject
        {
            
            public IList<ScratchData> ListOfProjects
            {
                get {
                    return scratchProjects;
                }
            }
        }

        internal void Fetch() {
            
            scratchWorker.DoWork += new DoWorkEventHandler(FetchScratch);
            scratchWorker.RunWorkerAsync();
        }

        private void FetchScratch(object sender, DoWorkEventArgs args) {
            
            FirestoreService firestoreService = new FirestoreService(new BaseClientService.Initializer
            {
                ApplicationName = "Sipp-PC",
                ApiKey = "AIzaSyCpPmG-AYByk3hircLVnAY_MLcp5ytIsAI",
            }
            );

            ProjectsResource.DatabasesResource.DocumentsResource.ListRequest req = firestoreService.Projects.Databases.Documents.List("projects/sipp-1f4c4/databases/(default)/documents/products/scratch", "projects");
            try
            {
                ListDocumentsResponse rsp = req.Execute();
                scratchProjects = new List<ScratchData>();

                foreach (Document project in rsp.Documents)
                {
                   
                    string title = getStringValue(project, "name");
                    string coverUrl = getStringValue(project, "cover_url");
                    string description = getStringValue(project, "description");
                    string scratchUrl = getStringValue(project, "scratch_url");
                    string docUrl = getStringValue(project, "doc_url");
                    scratchProjects.Add(new ScratchData(title, coverUrl, description, scratchUrl, docUrl));
                }
                
            }
            catch (Google.GoogleApiException e)
            {
             
                MessageBox.Show(e.ToString());
            }
        }

        private string getStringValue(Document doc, string field)
        {
            Value val = new Value();
            return doc.Fields.TryGetValue(field, out val) ? val.StringValue : "";
        }
    }

}
