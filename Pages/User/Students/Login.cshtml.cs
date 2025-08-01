using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Firebase.Database.Query;

namespace api.Pages.User.Students
{
    public class LoginModel : PageModel
    {
        [BindProperty]
        public string Username { get; set; }

        [BindProperty]
        public string Password { get; set; }

        public dynamic StudentInfo { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            Username = Request.Form["username"];
            Password = Request.Form["password"];
            var isTeacherLogin = !string.IsNullOrEmpty(Request.Form["teacherLogin"]);

            if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
            {
                ModelState.AddModelError(string.Empty, "Vui lòng nhập đầy đủ tài khoản và mật khẩu.");
                return Page();
            }

            using (var httpClient = new HttpClient())
            {
                
                
                    // Đăng nhập cho Student (giữ nguyên)
                    var response = await httpClient.GetAsync("https://api-ielts-cgn8.onrender.com/api/Student/all");
                    if (!response.IsSuccessStatusCode)
                    {
                        ModelState.AddModelError(string.Empty, "Không thể kết nối tới hệ thống.");
                        return Page();
                    }
                    var json = await response.Content.ReadAsStringAsync();
                    List<JObject> students = new();
                    try
                    {
                        students = JArray.Parse(json).ToObject<List<JObject>>();
                    }
                    catch
                    {
                        var dict = JObject.Parse(json);
                        foreach (var item in dict.Properties())
                        {
                            students.Add((JObject)item.Value);
                        }
                    }
                    foreach (var student in students)
                    {
                        var storedUsername = student["username"]?.ToString();
                        var storedPassword = student["password"]?.ToString();

                        if (storedUsername == Username && storedPassword == Password)
                        {
                            // Gán session cho Student
                            HttpContext.Session.SetString("StudentId", student["studentId"]?.ToString() ?? "");
                            HttpContext.Session.SetString("StudentName", student["name"]?.ToString() ?? "");
                            HttpContext.Session.SetString("StudentAvatar", student["avatar"]?.ToString() ?? "");
                            HttpContext.Session.SetString("StudentEmail", student["email"]?.ToString() ?? "");
                            HttpContext.Session.SetString("StudentClass", student["class"]?.ToString() ?? "");
                            var firebaseClient = new Firebase.Database.FirebaseClient("https://ielts-7d51b-default-rtdb.asia-southeast1.firebasedatabase.app/");

                            await firebaseClient
                                .Child("users")
                                .Child(student["studentId"]?.ToString())
                                .PutAsync(new
                                {
                                    userId = student["studentId"]?.ToString(),
                                    name = student["name"]?.ToString(),
                                    email = student["email"]?.ToString(),
                                    avatar = student["avatar"]?.ToString(),
                                    role = "student",
                                    classIds = new List<string> { student["class"]?.ToString() }
                                });

                            return RedirectToPage("/User/Students/Index");
                        }
                    }
                    ModelState.AddModelError(string.Empty, "Tài khoản hoặc mật khẩu không đúng.");
                    return Page();
                
            }
        }
    }
}
