using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using System.IO;
using System.Web.Http.Cors;
using NLog;

namespace file_upload_api.Controllers {
    public class UploadController : ApiController {

        private ILogger mLogger = LogManager.GetLogger("UploadController");

        // GET api/values
        public IEnumerable<string> Get() {
            return new string[] { "value1", "value2" };
        }

        // GET api/values/5
        public string Get(int id) {
            return "value";
        }

        // POST api/values
        [EnableCors(origins: "*", headers: "*", methods: "*", SupportsCredentials = true)]
        public HttpResponseMessage Post() {
            return Save();
        }

        // PUT api/values/5
        public void Put(int id, [FromBody]string value) {
        }

        // DELETE api/values/5
        public void Delete(int id) {
        }

        private HttpResponseMessage Save() {
            try {
                if (HttpContext.Current.Request.Files.AllKeys.Length > 0) {
                    var httpPostedChunkFile = HttpContext.Current.Request.Files["chunkFile"];
                    if (httpPostedChunkFile != null) {
                        mLogger.Debug("Handling chunk");
                        var saveFile = @"C:\Temp\UploadingFiles";
                        // Save the chunk file in temporery location with .part extension
                        var SaveFilePath = Path.Combine(saveFile, httpPostedChunkFile.FileName + ".part");
                        var chunkIndex = HttpContext.Current.Request.Form["chunkIndex"];
                        if (chunkIndex == "0") {
                            var fileType = HttpContext.Current.Request.Form["fileType"];
                            mLogger.Debug("Saving first chunk: fileType {0}", fileType);
                            httpPostedChunkFile.SaveAs(SaveFilePath);
                        } else {
                            var fileType = HttpContext.Current.Request.Form["fileType"];
                            mLogger.Debug("Saving chunk > 1: fileType {0}", fileType);
                            // Merge the current chunk file with previous uploaded chunk files
                            MergeChunkFile(SaveFilePath, httpPostedChunkFile.InputStream);
                            var totalChunk = HttpContext.Current.Request.Form["totalChunk"];
                            if (Convert.ToInt32(chunkIndex) == (Convert.ToInt32(totalChunk) - 1)) {
                                var savedFile = @"C:\Temp\UploadedFiles";
                                var originalFilePath = Path.Combine(savedFile, httpPostedChunkFile.FileName);
                                // After all the chunk files completely uploaded, remove the .part extension and move this file into save location
                                System.IO.File.Move(SaveFilePath, originalFilePath);
                            }
                        }
                        return Request.CreateResponse(HttpStatusCode.Created, string.Format("Uploaded chunk {0}", chunkIndex));
                    }
                    var httpPostedFile = HttpContext.Current.Request.Files["UploadFiles"];

                    if (httpPostedFile != null) {
                        var fileType = HttpContext.Current.Request.Form["fileType"];
                        mLogger.Debug("Handling file (non-chunked): fileType {0}", fileType);

                        var fileSave = @"C:\Temp\UploadedFiles";
                        var fileSavePath = Path.Combine(fileSave, httpPostedFile.FileName);
                        if (!File.Exists(fileSavePath)) {
                            httpPostedFile.SaveAs(fileSavePath);
                            return Request.CreateResponse(HttpStatusCode.Created, "File uploaded");
                        } else {
                            return Request.CreateResponse(HttpStatusCode.InternalServerError, "File already exists");
                        }
                    }
                }
            }
            catch (Exception e) {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, e.Message);
            }
            return Request.CreateResponse(HttpStatusCode.BadRequest, "Nothing to process (chunked or posted file)");
        }

        private void MergeChunkFile(string fullPath, Stream chunkContent) {
            try {
                using (FileStream stream = new FileStream(fullPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite)) {
                    using (chunkContent) {
                        chunkContent.CopyTo(stream);
                    }
                }
            }
            catch (IOException ex) {
                throw ex;
            }
        }
    }
}
