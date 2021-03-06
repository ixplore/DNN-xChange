﻿//
// DotNetNuke® - http://www.dotnetnuke.com
// Copyright (c) 2002-2012
// by DotNetNuke Corporation
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and 
// to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions 
// of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED 
// TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using DotNetNuke.Common.Utilities;
using DotNetNuke.DNNQA.Providers.Data.SqlDataProvider;
using DotNetNuke.Security;
using DotNetNuke.Services.Localization;
using DotNetNuke.Web.Mvp;
using DotNetNuke.DNNQA.Components.Controllers;
using DotNetNuke.DNNQA.Components.Models;
using DotNetNuke.DNNQA.Components.Views;
using DotNetNuke.DNNQA.Components.Common;
using System.Web.UI.WebControls;
using DotNetNuke.DNNQA.Components.Entities;

namespace DotNetNuke.DNNQA.Components.Presenters
{

	/// <summary>
	/// 
	/// </summary>
	public class BrowsePresenter : ModulePresenter<IBrowseView, BrowseModel>
	{

		#region Members

		protected IDnnqaController Controller { get; private set; }
		
		/// <summary>
		/// 
		/// </summary>
		private string Keyword
		{
			get
			{
				var keyword = Null.NullString;
				if (!String.IsNullOrEmpty(Request.Params["keyword"]))
				{
					keyword = (Request.Params["keyword"]);
				}
				return keyword;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		private int Page
		{
			get
			{
				var page = 0;
				if (!String.IsNullOrEmpty(Request.Params["page"])) page = Convert.ToInt32(Request.Params["page"]);

				return page;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		private int PageSize
		{
			get
			{
				var pageSize = Constants.DefaultPageSize;
				if (ModuleContext.Settings.ContainsKey(Constants.SettingBrowseQPageSize))
				{
					pageSize = Convert.ToInt32(ModuleContext.Settings[Constants.SettingBrowseQPageSize]);
				}

				return pageSize;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		private string SearchType
		{
			get
			{
				var controlView = Null.NullString;
				if (!String.IsNullOrEmpty(Request.Params["type"]))
				{
					controlView = (Request.Params["type"]);
				}
				return controlView;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		private int SearchUser
		{
			get
			{
				var user = Null.NullInteger;
				if (!String.IsNullOrEmpty(Request.Params["user"]))
				{
					user = Int32.Parse(Request.Params["user"]);
				}
				return user;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		private string Sort
		{
			get
			{
				var sort = Null.NullString;
				if (!String.IsNullOrEmpty(Request.Params["sort"]))
				{
					sort = (Request.Params["sort"]);
				}
				return sort;
			}
		}

		/// <summary>
		/// The tag we want to search for (based on a parameter in the URL). 
		/// </summary>
		private string Tag
		{
			get
			{
				var tag = Null.NullString;
				if (!String.IsNullOrEmpty(Request.Params["tag"])) tag = (Request.Params["tag"]);
				var objSecurity = new PortalSecurity();

				return objSecurity.InputFilter(tag, PortalSecurity.FilterFlag.NoSQL);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		private int TagPageSize
		{
			get
			{
				var pageSize = Constants.DefaultPageSize;
				if (ModuleContext.Settings.ContainsKey(Constants.SettingHomeMaxTags))
				{
					pageSize = Convert.ToInt32(ModuleContext.Settings[Constants.SettingHomeMaxTags]);
				}

				return pageSize;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		private Constants.TagMode TagTimeFrame
		{
			get
			{
				if (ModuleContext.Settings.ContainsKey(Constants.SettingHomeTagTimeFrame))
				{
					switch (Convert.ToInt32(ModuleContext.Settings[Constants.SettingHomeTagTimeFrame]))
					{
						case (int)Constants.TagMode.ShowTotalUsage:
							return Constants.TagMode.ShowTotalUsage;
						case (int)Constants.TagMode.ShowMonthlyUsage:
							return Constants.TagMode.ShowMonthlyUsage;
						case (int)Constants.TagMode.ShowWeeklyUsage:
							return Constants.TagMode.ShowWeeklyUsage;
						//case (int)Constants.TagMode.ShowNoUsage:
						//    e.TagControl.CountMode = Constants.TagMode.ShowNoUsage;
						//    break;
						default:
							return Constants.TagMode.ShowDailyUsage;
					}
				}
				return Constants.TagMode.ShowDailyUsage;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		private int TotalRecords { get; set; }

		/// <summary>
		/// 
		/// </summary>
		private bool Unanswered
		{
			get
			{
				var response = false;
				if (!String.IsNullOrEmpty(Request.Params["unanswered"]))
				{
					var unanswered = (Request.Params["unanswered"]);
					if (unanswered == "true")
					{
						response = true;
					}
				}

				return response;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		private UserScoreInfo UserScore
		{
			get
			{
				if (ModuleContext.PortalSettings.UserId > 0)
				{
					var usersScore = Controller.GetUserScore(ModuleContext.PortalSettings.UserId, ModuleContext.PortalId);
					if (usersScore != null)
					{
						return usersScore;
					}
				}
				var objUserScore = new UserScoreInfo
				{
					Message = "",
					PortalId = ModuleContext.PortalId,
					UserId = ModuleContext.PortalSettings.UserId,
					Score = 0
				};
				return objUserScore;
			}
		}

		/// <summary>
		/// TODO: Tie this to a module setting.
		/// </summary>
		private int VocabularyId
		{
			get { return 1; }
		}

		/// <summary>
		/// GroupID if available
		/// </summary>
		private int GroupId
		{
			get { return (Request.QueryString["groupid"] == null) ? 0 : int.Parse(Request.QueryString["groupid"].ToString()); }
		}

		#endregion

		#region Constructors

		/// <summary>
		/// 
		/// </summary>
		/// <param name="view"></param>
		public BrowsePresenter(IBrowseView view)
			: this(view, new DnnqaController(new SqlDataProvider()))
		{
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="view"></param>
		/// <param name="controller"></param>
		public BrowsePresenter(IBrowseView view, IDnnqaController controller)
			: base(view)
		{
			if (view == null)
			{
				throw new ArgumentException(@"View is nothing.", "view");
			}

			if (controller == null)
			{
				throw new ArgumentException(@"Controller is nothing.", "controller");
			}

			Controller = controller;
			Controller.QACacheTimout = Convert.ToInt32(ModuleContext.Settings[Constants.SettingsCacheTimeout]);
			View.Load += ViewLoad;
		}

		#endregion

		#region Event Handlers

		/// <summary>
		/// 
		/// </summary>
		protected void ViewLoad(object sender, EventArgs eventArgs)
		{
			try
			{
				SetSearchFilters();

				var objSort = new SortInfo { Column = "active", Direction = Constants.SortDirection.Descending };

				if (Sort != Null.NullString)
				{
					switch (Sort.ToLower())
					{
						case "newest":
							objSort.Column = "newest";
							break;
						case "votes":
							objSort.Column = "votes";
							break;
						default:
							objSort.Column = "active";
							break;
					}
				}

				View.Model.SortBy = Sort;
				View.Model.ApplyUnanswered = Unanswered;				
				View.Model.PageTitle = Localization.GetString("BrowseMetaTitle", LocalResourceFile);
				View.Model.PageDescription = Localization.GetString("BrowseMetaDescription", LocalResourceFile);
				View.Model.PageLink = Links.ViewQuestions(ModuleContext,GroupId);
				View.Model.ColQuestions = SearchResults(Page);

				View.PagerChanged += PagerChanged;
				View.ItemDataBound += ItemDataBound;
				View.TagItemDataBound += TagItemDataBound;
				View.DashboardDataBound += DashboardDataBound;

				View.Refresh();
			}
			catch (Exception exc)
			{
				ProcessModuleLoadException(exc);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		protected void ItemDataBound(object sender, HomeQuestionsEventArgs<HyperLink, QuestionInfo, Literal, Literal, Literal, Literal, Literal, Panel, Controls.Tags, Image> e)
		{
			var control = (Control)sender;

			e.TitleLink.Text = e.ObjQuestion.Title;
			// because this view can show both questions and answers (all posts), we need to make sure we are generating a proper link to the question itself.
			var questionId = e.ObjQuestion.ParentId != 0 ? e.ObjQuestion.ParentId : e.ObjQuestion.PostId;
			var _groupLink = Request.QueryString["groupid"] ?? "0";
			e.TitleLink.NavigateUrl = Links.ViewQuestion(questionId, e.ObjQuestion.Title, ModuleContext.PortalSettings.ActiveTab,
				ModuleContext.PortalSettings, _groupLink == "0" ? 0 : int.Parse(_groupLink));

			if (e.ObjQuestion.TotalAnswers > 0)
			{
				e.AnswersPanel.CssClass = "answers multiple";
			}

			if (e.ObjQuestion.AnswerId > 0)
			{
				e.AnswersLiteral.Text = @"<span class='count accepted'>" + e.ObjQuestion.TotalAnswers + @"</span>";
				e.AnswersTextLiteral.Text = @"<span class='accepted'>" + Localization.GetString("Answers", LocalResourceFile) + @"</span>";
			}
			else
			{
				e.AnswersLiteral.Text = @"<span class='count'>" + e.ObjQuestion.TotalAnswers + @"</span>";
				e.AnswersTextLiteral.Text = @"<span>" + Localization.GetString("Answers", LocalResourceFile) + @"</span>";
			}

			e.ViewsLiteral.Text = e.ObjQuestion.ViewCount.ToString();
			e.VotesLiteral.Text = e.ObjQuestion.QuestionVotes.ToString();

			e.Tags.ModContext = ModuleContext;
			e.Tags.ModuleTab  = ModuleContext.PortalSettings.ActiveTab;
			e.Tags.GroupId = GroupId;
			e.Tags.DataSource = e.ObjQuestion.QaTerms(VocabularyId);
			e.Tags.DataBind();

			var objUser = DotNetNuke.Entities.Users.UserController.GetUserById(ModuleContext.PortalId, e.ObjQuestion.LastApprovedUserId);
			e.DateLiteral.Text = Utils.CalculateDateForDisplay(e.ObjQuestion.LastApprovedDate) + @" <a href=" + DotNetNuke.Common.Globals.UserProfileURL(e.ObjQuestion.LastApprovedUserId) + @">" + objUser.DisplayName + @"</a>";

			e.AcceptedImage.Visible       = e.ObjQuestion.AnswerId > 0;
			e.AcceptedImage.ImageUrl      = control.ResolveUrl("~/DesktopModules/DNNQA/images/accepted.png");
			e.AcceptedImage.AlternateText = Localization.GetString("imgAccepted", Constants.SharedResourceFileName);
			e.AcceptedImage.ToolTip       = Localization.GetString("AcceptedAnswer", Constants.SharedResourceFileName);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		protected void PagerChanged(object sender, PagerChangedEventArgs<LinkButton, string> e)
		{
			View.Model.CurrentPage += Convert.ToInt32(e.LinkButton.CommandArgument);
			View.Model.ColQuestions = SearchResults(View.Model.CurrentPage);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		protected void TagItemDataBound(object sender, HomeTagsEventArgs<TermInfo, Controls.Tags> e)
		{
			var colTerms = new List<TermInfo> { e.Term };

			e.TagControl.ModContext = ModuleContext;
			e.TagControl.ModuleTab = ModuleContext.PortalSettings.ActiveTab;
			e.TagControl.GroupId = GroupId;
			e.TagControl.DataSource = colTerms;
			e.TagControl.ModuleTab = ModuleContext.PortalSettings.ActiveTab;
			e.TagControl.CountMode = TagTimeFrame;
			e.TagControl.DataBind();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		protected void DashboardDataBound(object sender, HomeUserEventArgs<HtmlGenericControl, HtmlGenericControl, HtmlGenericControl, HtmlGenericControl, Literal, Literal, Literal, Literal> e)
		{
			e.DashHeader.Visible = ModuleContext.PortalSettings.UserId > 0;
			e.DashList.Visible = ModuleContext.PortalSettings.UserId > 0;

			e.FavoriteTagsHead.Visible = false;
			e.FavoriteTagsList.Visible = false;

			var dashCount = Localization.GetString("dashCount", LocalResourceFile);
			var colAnswers = Controller.GetUserAnswers(ModuleContext.PortalId, ModuleContext.PortalSettings.UserId);
			e.AnswerLiteral.Text = dashCount.Replace("{0}", colAnswers.Count.ToString());

			var colQuestions = Controller.GetUserQuestions(ModuleContext.PortalId, ModuleContext.PortalSettings.UserId);
			e.QuestionLiteral.Text = dashCount.Replace("{0}", colQuestions.Count.ToString());

			var colSubs = Controller.GetUserSubscriptions(ModuleContext.PortalId, ModuleContext.PortalSettings.UserId);
			e.SubscriptionLiteral.Text = dashCount.Replace("{0}", colSubs.Count.ToString());

			e.ScoreLiteral.Text = Localization.GetString("userRep", LocalResourceFile).Replace("{0}", UserScore.Score.ToString());
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// 
		/// </summary>
		private void SetSearchFilters()
		{
			var applyTag = Tag.Length > 0;
			var applyType = SearchType.Length > 0;
			var applyUser = SearchUser > 0;
			var applyKeyword = Keyword.Length > 0;
			var applyUnanswered = Unanswered;

			View.Model.AppliedFilters = new List<FilterInfo>();

			if (applyTag)
			{
				var urlTerm =
					(from t in Controller.GetTermsByContentType(ModuleContext.PortalId, ModuleContext.ModuleId, VocabularyId, GroupId)
					 where t.Name.ToLower() == Tag.ToLower()
					 select t).SingleOrDefault();

				if (urlTerm != null)
				{
					View.Model.TagDetailUrl = Links.ViewTagDetail(ModuleContext, ModuleContext.TabId, urlTerm.Name, GroupId);
					View.Model.SelectedTerm = urlTerm;

					var objTagFilter = new FilterInfo
					{
						FilterName = urlTerm.Name,
						FilterType = Constants.SearchFilterType.Tag,
						RemoveFilterLink = "#"
					};

					View.Model.AppliedFilters.Add(objTagFilter);
				}
			}

			if (applyType)
			{
				var objTypeFilter = new FilterInfo
				{
					FilterName = SearchType,
					FilterType = Constants.SearchFilterType.Type,
					RemoveFilterLink = "#"
				};

				View.Model.AppliedFilters.Add(objTypeFilter);
			}

			if (applyUser)
			{
				var objUserFilter = new FilterInfo
				{
					FilterName = SearchUser.ToString(),
					FilterType = Constants.SearchFilterType.User,
					RemoveFilterLink = "#"
				};

				View.Model.AppliedFilters.Add(objUserFilter);
				View.Model.AppliedUser = SearchUser;
				View.Model.AppliedType = SearchType;
			}

			if (applyKeyword)
			{
				var objKeywordFilter = new FilterInfo
				{
					FilterName = Keyword.Trim(),
					FilterType = Constants.SearchFilterType.Content,
					RemoveFilterLink = "#"
				};

				View.Model.AppliedFilters.Add(objKeywordFilter);
				View.Model.AppliedKeyword = Keyword.Trim();
			}

			if (applyUnanswered)
			{
				var objUnansweredFilter = new FilterInfo
				{
					FilterName = "unanswered",
					FilterType = Constants.SearchFilterType.Unanswered,
					RemoveFilterLink = "#"
				};

				View.Model.AppliedFilters.Add(objUnansweredFilter);
			}
		}

		/// <summary>
		/// Returns a collection of questions, sorted and showing a specific 'page'. Also handles display of paging related buttons (like previous/next). 
		/// </summary>
		/// <returns></returns>
		private List<QuestionInfo> SearchResults(int currentPage)
		{
			List<QuestionInfo> results;
			var tagResults = new List<TermInfo>();
			var applyTag = Tag.Length > 0;
			var applyType = SearchType.Length > 0;
			var applyUser = SearchUser > 0;
			var applyKeyword = Keyword.Length > 0;
			var applyUnanswered = Unanswered;

			var objSort = new SortInfo { Column = "active", Direction = Constants.SortDirection.Descending };

			var colModuleTags = Controller.GetTermsByContentType(ModuleContext.PortalId, ModuleContext.ModuleId, VocabularyId, GroupId);

			IEnumerable<TermInfo> colTags;
			switch (TagTimeFrame)
			{
				case Constants.TagMode.ShowDailyUsage:
					colTags = (from t in colModuleTags where t.DayTermUsage > 0 select t);
					break;
				case Constants.TagMode.ShowWeeklyUsage:
					colTags = (from t in colModuleTags where t.WeekTermUsage > 0 select t);
					break;
				case Constants.TagMode.ShowMonthlyUsage:
					colTags = (from t in colModuleTags where t.MonthTermUsage > 0 select t);
					break;
				default:
					colTags = (from t in colModuleTags where t.TotalTermUsage > 0 select t);
					break;
			}

			if (Sort != Null.NullString)
			{
				switch (Sort.ToLower())
				{
					case "oldest":
						objSort.Column = "oldest";
						objSort.Direction = Constants.SortDirection.Ascending;
						break;
					case "votes":
						objSort.Column = "votes";
						break;
					default:
						objSort.Column = "active";
						break;
				}
			}

			if (applyTag)
			{
				results = Controller.TermSearch(ModuleContext.ModuleId, 1000, Tag, GroupId);
				tagResults.Add(View.Model.SelectedTerm);

				TotalRecords = results.Count() > 0 ? results[0].TotalRecords : 0;
				View.Model.HeaderTitle = Localization.GetString("TaggedQuestions", LocalResourceFile);

				if (applyUnanswered)
				{
					results = (from t in results where t.TotalAnswers < 1 select t).ToList();
					TotalRecords = results.Count() > 0 ? results.Count() : 0;
				}
			}
			else if (applyUser)
			{
				if (applyType)
				{
					switch (SearchType)
					{
						// we could use currentuserid against userid in url to determine if we should show my or username "Answers" or "Questions". 
						case "answers":
							results = Controller.GetUserAnswers(ModuleContext.PortalId, SearchUser);
							TotalRecords = results.Count() > 0 ? results[0].TotalRecords : 0;

							View.Model.HeaderTitle = Localization.GetString("MyAnswers", LocalResourceFile);



							if (applyUnanswered)
							{
								results = (from t in results where t.TotalAnswers < 1 select t).ToList();
								TotalRecords = results.Count() > 0 ? results.Count() : 0;
							}

							// tags
							tagResults = Sorting.GetHomeTermCollection(TagPageSize, TagTimeFrame, colTags).ToList();

							break;
						default:
							results = Controller.GetUserQuestions(ModuleContext.PortalId, SearchUser);
							TotalRecords = results.Count() > 0 ? results[0].TotalRecords : 0;
							View.Model.HeaderTitle = Localization.GetString("MyQuestions", LocalResourceFile);

							if (applyUnanswered)
							{
								results = (from t in results where t.TotalAnswers < 1 select t).ToList();
								TotalRecords = results.Count() > 0 ? results.Count() : 0;
							}

							// tags
							tagResults = Sorting.GetHomeTermCollection(TagPageSize, TagTimeFrame, colTags).ToList();

							break;
					}
				}
				else
				{
					results = Controller.SearchByUser(ModuleContext.PortalId, SearchUser);
					TotalRecords = results.Count() > 0 ? results[0].TotalRecords : 0;
					View.Model.HeaderTitle = Localization.GetString("UserSpecific", LocalResourceFile);

					if (applyUnanswered)
					{
						results = (from t in results where t.TotalAnswers < 1 select t).ToList();
						TotalRecords = results.Count() > 0 ? results.Count() : 0;
					}
				}
			}
			else
			{
				if (applyKeyword)
				{
					results = Controller.KeywordSearch(ModuleContext.ModuleId, Keyword, GroupId);
					TotalRecords = results.Count() > 0 ? results[0].TotalRecords : 0;
					View.Model.HeaderTitle = Localization.GetString("Keyword", LocalResourceFile);

					if (applyUnanswered)
					{
						results = (from t in results where t.TotalAnswers < 1 select t).ToList();
						TotalRecords = results.Count() > 0 ? results.Count() : 0;
					}

					tagResults = Sorting.GetHomeTermCollection(TagPageSize, TagTimeFrame, colTags).ToList();
				}
				else
				{
					results = Controller.KeywordSearch(ModuleContext.ModuleId, Null.NullString, GroupId);
					TotalRecords = results.Count() > 0 ? results[0].TotalRecords : 0;
					View.Model.HeaderTitle = Localization.GetString("LatestQuestions", LocalResourceFile);

					if (applyUnanswered)
					{
						results = (from t in results where t.TotalAnswers < 1 select t).ToList();
						TotalRecords = results.Count() > 0 ? results.Count() : 0;
					}

					tagResults = Sorting.GetHomeTermCollection(TagPageSize, TagTimeFrame, colTags).ToList();
				}
			}

			// Related Terms
			View.Model.RelatedTags = tagResults;

			var resultsCollection = Sorting.GetKeywordSearchCollection(PageSize, currentPage, objSort, results).ToList();			
			var totalPages = Convert.ToDouble((double)TotalRecords / PageSize);

			if ((totalPages > 1) && (totalPages > currentPage + 1))
			{
				View.ShowNextButton(true);
				View.Model.NextPageLink = Links.ViewQuestionsPaged(ModuleContext, View.Model.CurrentPage + 1, GroupId);
			}
			else
			{
				View.ShowNextButton(false);
			}

			if ((totalPages > 1) && (currentPage > 0))
			{
				View.ShowBackButton(true);
				View.Model.PrevPageLink = Links.ViewQuestionsPaged(ModuleContext, View.Model.CurrentPage - 1, GroupId);
			}
			else
			{
				View.ShowBackButton(false);
			}

			return resultsCollection;
		}

		#endregion

	}
}