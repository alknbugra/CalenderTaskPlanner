using CalenderDemo2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Services.Description;
using System.Web.UI.WebControls;

namespace CalenderDemo2.Controllers
{
    public class HomeController : Controller
    {

        ContexDB db = new ContexDB();
        private static readonly bool DemoMode =
            (System.Configuration.ConfigurationManager.AppSettings["DemoMode"] ?? "false").ToLowerInvariant() == "true";

        private static List<UserTbl> _users = new List<UserTbl>();
        private static List<MainTicketTbl> _AllList = new List<MainTicketTbl> { };
        private static List<CalendarEvent> _eventsAssigned = new List<CalendarEvent> { };
        private static List<CalendarEvent> _eventsUnassigned = new List<CalendarEvent> { };


        // Constractor
        public ActionResult Index()
        {
            GetDevelopers();

            if (_users.Count == 0)
            {
                ViewBag.SystemDevelopersList = null;
            }
            else
            {
                ViewBag.SystemDevelopersList = _users;
            }

            return View();
        }


        // Sol Menü Task
        public void GetDevelopers()
        {
            _users.Clear();
            if (DemoMode)
            {
                _users.AddRange(new List<UserTbl>
                {
                    new UserTbl { Id = 1, Name = "Ali", Surname = "Yılmaz", ShowStatus = true },
                });
            }
            else
            {
                _users.AddRange(db.UserTbl.Where(x => x.ShowStatus == true && x.UserUnitTbl.Where(a => a.UnitId == 1 && a.UserId == x.Id).FirstOrDefault() != null).ToList());
            }
        }
        public PartialViewResult P_GetDeveloperPlan(int DeveloperId)
        {
            // In demo mode, keep in-memory tasks so drag/drop changes persist across requests
            if (!DemoMode)
            {
                _AllList.Clear();
            }
            _eventsAssigned.Clear();
            _eventsUnassigned.Clear();

            if (DemoMode)
            {
                // Initialize demo data for this developer only once
                if (!_AllList.Any(x => x.AssigneeUserId == DeveloperId))
                {
                    _AllList.AddRange(new List<MainTicketTbl>
                    {
                        new MainTicketTbl { Id = 101, TaskId = 1001, Summary = "Giriş ekranı düzenlemesi", StatusId = 10, TicketStatusTbl = new TicketStatusTbl { StatusDescription = "To Do", BackgroundColor = "#e3e3e3", FontColor = "#000000", UnresolvedStatus = true }, DeletedStatus = false, AssigneeUserId = DeveloperId, PlanningStartDate = null, PlaningEndDate = null },
                        new MainTicketTbl { Id = 102, TaskId = 1002, Summary = "API performans iyileştirme", StatusId = 20, TicketStatusTbl = new TicketStatusTbl { StatusDescription = "In Progress", BackgroundColor = "#ffe08a", FontColor = "#000000", UnresolvedStatus = true }, DeletedStatus = false, AssigneeUserId = DeveloperId, PlanningStartDate = DateTime.Today, PlaningEndDate = DateTime.Today.AddDays(2) },
                        new MainTicketTbl { Id = 103, TaskId = 1003, Summary = "Hata bildirimi düzeni", StatusId = 30, TicketStatusTbl = new TicketStatusTbl { StatusDescription = "Review", BackgroundColor = "#6F61C4", FontColor = "#ffffff", UnresolvedStatus = true }, DeletedStatus = false, AssigneeUserId = DeveloperId, PlanningStartDate = DateTime.Today.AddDays(3), PlaningEndDate = DateTime.Today.AddDays(4) }
                    });
                }
                // Build event lists for this developer
                RebuildEventsForDeveloper(DeveloperId);
            }
            else
            {
                _AllList = db.MainTicketTbl.Where(a => a.DeletedStatus == false && a.AssigneeUserId == DeveloperId && a.TicketStatusTbl.UnresolvedStatus == true).OrderBy(x => x.PlanningStartDate).ToList();
                RebuildEventsForDeveloper(DeveloperId);
            }

            return PartialView(_eventsUnassigned);

        }
        public PartialViewResult P_GetRecordsFilterList()
        {
            var GroupList = _eventsUnassigned.GroupBy(x => x.statusId).Select(g => new ListItem
            {
                Value = g.Key.ToString(),
                Text = g.FirstOrDefault().status,
            }).ToList();

            return PartialView(GroupList);
        }
        public PartialViewResult P_SetFilterStatus(int IdStatus)
        {
            if (IdStatus == 0) // Tümü
            {
                return PartialView(_eventsUnassigned);
            }
            else
            {
                var FilterList = _eventsUnassigned.Where(c => c.statusId == IdStatus).ToList();
                return PartialView(FilterList);
            }
        }


        // Sağ Menü Takvim
        public JsonResult P_GetDeveloperEvents()
        {
            var events = _eventsAssigned.Select(x => new CalendarEvent
            {
                id = x.id,
                taskid = x.taskid,
                title = x.shorttitle,
                start = x.start,
                end = x.end,
                allDay = true,
                backgroundColor = x.backgroundColor,
                textColor = x.textColor,
                borderColor = x.borderColor,
                shorttitle = x.shorttitle,
                status = x.status,
                statusId = x.statusId,
                url = x.url
            });
            return Json(events.ToList(), JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public JsonResult CreateEvent(int id, string start, string end)
        {
            if (DemoMode)
            {
                var record = _AllList.FirstOrDefault(c => c.Id == id);
                if (record != null)
                {
                    record.PlanningStartDate = DateTime.Parse(start);
                    record.PlaningEndDate = DateTime.Parse(end);
                }
                // Keep event lists in sync for current assignee
                if (record != null && record.AssigneeUserId.HasValue)
                {
                    RebuildEventsForDeveloper(record.AssigneeUserId.Value);
                }
                return Json(new { success = true });
            }
            try
            {
                DateTime startDate = DateTime.Parse(start);
                DateTime endDate = DateTime.Parse(end);

                // db update
                using (var context = new ContexDB())
                {
                    var Record = context.MainTicketTbl.FirstOrDefault(c => c.Id == id);
                    if (Record != null)
                    {
                        Record.PlanningStartDate = startDate;
                        Record.PlaningEndDate = endDate;
                        context.SaveChanges();

                        _ = UpdateMainTask(Record.SubtaskMainTicketId);
                    }
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        [HttpPost]
        public JsonResult UpdateEventDateRange(int? id, string start, string end)
        {
            if (DemoMode)
            {
                var record = _AllList.FirstOrDefault(c => c.Id == id);
                if (record != null)
                {
                    record.PlanningStartDate = DateTime.Parse(start);
                    record.PlaningEndDate = DateTime.Parse(end);
                }
                // Keep event lists in sync for current assignee
                if (record != null && record.AssigneeUserId.HasValue)
                {
                    RebuildEventsForDeveloper(record.AssigneeUserId.Value);
                }
                return Json(new { success = true });
            }
            try
            {
                DateTime startDate = DateTime.Parse(start);
                DateTime endDate = DateTime.Parse(end);

                // db update
                using (var context = new ContexDB())
                {
                    var Record = context.MainTicketTbl.FirstOrDefault(c => c.Id == id);
                    if (Record != null)
                    {
                        Record.PlanningStartDate = startDate;
                        Record.PlaningEndDate = endDate;
                        context.SaveChanges();

                        _ = UpdateMainTask(Record.SubtaskMainTicketId);
                    }
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult DeleteEvent(int id)
        {
            if (DemoMode)
            {
                var record = _AllList.FirstOrDefault(c => c.Id == id);
                if (record != null)
                {
                    record.PlanningStartDate = null;
                    record.PlaningEndDate = null;
                }
                // Keep event lists in sync for current assignee
                if (record != null && record.AssigneeUserId.HasValue)
                {
                    RebuildEventsForDeveloper(record.AssigneeUserId.Value);
                }
                return Json(new { success = true });
            }
            try
            {
                // db update
                using (var context = new ContexDB())
                {
                    var Record = context.MainTicketTbl.FirstOrDefault(c => c.Id == id);
                    if (Record != null)
                    {
                        Record.PlanningStartDate = null;
                        Record.PlaningEndDate = null;
                        context.SaveChanges();

                        _ = UpdateMainTask(Record.SubtaskMainTicketId);
                    }
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        async Task UpdateMainTask(int? SubtaskMainTicketId)
        {
            if (DemoMode)
            {
                await Task.CompletedTask;
                return;
            }
            Task task = Task.Run(() =>
            {

                using (var context = new ContexDB())
                {

                    var ListSubtask = context.MainTicketTbl.Where(c => c.StatusId != 26 && (c.SubtaskMainTicketId == SubtaskMainTicketId || c.MainTicketId == SubtaskMainTicketId)).ToList();
                    var MinDate = ListSubtask.Min(c => c.PlanningStartDate).Value;
                    var MaxDate = ListSubtask.Max(c => c.PlaningEndDate).Value;

                    var AnaKayit = context.MainTicketTbl.Where(c => c.Id == SubtaskMainTicketId).FirstOrDefault();
                    AnaKayit.PlanningStartDate = MinDate;
                    AnaKayit.PlaningEndDate = MaxDate;

                }

            });

            await Task.CompletedTask;
        }

        private void RebuildEventsForDeveloper(int developerId)
        {
            _eventsAssigned.Clear();
            _eventsUnassigned.Clear();

            foreach (var item in _AllList.Where(x => x.AssigneeUserId == developerId))
            {
                if (!DemoMode)
                {
                    if (item.MainTicketId != null)
                    {
                        var mainT = db.MainTicketTbl.Where(x => x.Id == item.MainTicketId).FirstOrDefault();
                        if (mainT != null)
                        {
                            item.ProjectId = mainT.ProjectId;
                        }
                    }
                }

                if (!_AllList.Where(c => c.SubtaskMainTicketId == item.Id).Any())
                {
                    if (item.PlanningStartDate == null)
                    {
                        var calendarEvent = new CalendarEvent
                        {
                            id = item.Id,
                            taskid = item.TaskId,
                            title = item.Summary,
                            shorttitle = $"QBT-{item.TaskId}/{item.Summary}",
                            start = null,
                            end = null,
                            allDay = false,
                            backgroundColor = item.TicketStatusTbl?.BackgroundColor ?? "#e3e3e3",
                            textColor = item.TicketStatusTbl?.FontColor ?? "#000000",
                            statusId = item.StatusId,
                            status = item.TicketStatusTbl?.StatusDescription ?? "To Do"
                        };
                        _eventsUnassigned.Add(calendarEvent);
                    }
                    else
                    {
                        var calendarEvent = new CalendarEvent
                        {
                            id = item.Id,
                            taskid = item.TaskId,
                            title = item.Summary,
                            shorttitle = $"QBT-{item.TaskId}/{item.Summary}",
                            start = item.PlanningStartDate?.ToString("yyyy-MM-dd"),
                            end = item.PlaningEndDate?.AddDays(1).ToString("yyyy-MM-dd"),
                            allDay = false,
                            backgroundColor = item.TicketStatusTbl?.BackgroundColor ?? "#e3e3e3",
                            textColor = item.TicketStatusTbl?.FontColor ?? "#000000",
                            statusId = item.StatusId,
                            status = item.TicketStatusTbl?.StatusDescription ?? "To Do"
                        };
                        _eventsAssigned.Add(calendarEvent);
                    }
                }
            }
        }


    }
}