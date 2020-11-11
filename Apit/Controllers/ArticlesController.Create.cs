﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BusinessLayer.DataServices;
using BusinessLayer.Models;
using DatabaseLayer.Entities;
using DatabaseLayer.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Apit.Controllers
{
    public partial class ArticlesController
    {
        [Authorize]
        public IActionResult Create()
        {
            var conference = _dataManager.Conferences.Current;
            if (conference == null || (!conference.Topics?.Any() ?? true))
                return RedirectToAction("index", "account");
            return View();
        }

        [HttpPost, Authorize]
        public async Task<IActionResult> Create(NewArticleViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // Combine all errors and return them back if this variable is set to true
            bool hasIncorrectData = false;

            #region ================ Long form review ================

            // Check selected topic existing
            var topic = model.TopicId == null ? null : _dataManager.Topics.GetById(Guid.Parse(model.TopicId));
            if (topic == null)
            {
                ModelState.AddModelError(nameof(model.TopicId),
                    "дана тема не може бути використана");
                hasIncorrectData = true;
            }

            // "hello, wor/*+#ld1!" != "hello, world1!"
            if (keyWordsAvailableRegex.Replace(model.KeyWords, "") != "")
            {
                ModelState.AddModelError(nameof(model.KeyWords),
                    "Unsupported character detected");
                hasIncorrectData = true;
            }

            // Check uploaded file
            string extension = default, uniqueAddress = default;
            if (model.DocFile.Length > 0)
            {
                extension = Path.GetExtension(model.DocFile.FileName);
                uniqueAddress = _dataManager.Articles.GenerateUniqueAddress();
                _logger.LogInformation("Upload file with extension: " + extension);

                if (!Regex.IsMatch(extension ?? "", @"\.docx?$"))
                {
                    ModelState.AddModelError(nameof(model.DocFile),
                        "невірний формат файлу (доступно лише .doc і .docx)");
                    hasIncorrectData = true;
                }
                else if (!hasIncorrectData)
                {
                    string err = await DataUtil.TrySaveDocFile(model.DocFile, uniqueAddress, extension);
                    if (err != null)
                    {
                        _logger.LogError("Document converter error\n" + err);
                        ModelState.AddModelError(nameof(model.DocFile),
                            "даний файл не може бути збереженим, оскільки може нести у собі загрозу для сервісу. " +
                            "Якщо це не так, будь ласка, зверніться до адміністрації сайту");
                        hasIncorrectData = true;
                    }
                }
            }
            else
            {
                ModelState.AddModelError(nameof(model.DocFile),
                    "будь ласка, прикрепіть файл з матеріалом");
                hasIncorrectData = true;
            }

            #endregion

            if (hasIncorrectData) return View(model);


            var dateNow = DateTime.Now;
            var user = await _userManager.GetUserAsync(User);

            var authors = new List<UserOwnArticlesLinking>
            {
                new UserOwnArticlesLinking
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    User = user
                }
            };

            var article = new Article
            {
                Id = Guid.NewGuid(),
                Topic = topic,
                Authors = authors,
                UniqueAddress = uniqueAddress,

                Title = model.Title,
                ShortDescription = model.ShortDescription,
                Status = ArticleStatus.Uploaded,
                KeyWords = keyWordsSeparatorRegex.Replace(model.KeyWords, ";"),

                HtmlFilePath = uniqueAddress + ".htm",
                DocxFilePath = uniqueAddress + extension,

                Conference = _dataManager.Conferences.GetCurrentAsDbModel(),

                DateCreated = dateNow,
                DateLastModified = dateNow
            };

            foreach (var author in article.Authors)
            {
                author.ArticleId = article.Id;
                author.Article = article;
            }

            var currentConf = _dataManager.Conferences.GetCurrentAsDbModel();
            _dataManager.Conferences.AddArticle(currentConf, article);
            _dataManager.Articles.Create(article);

            return RedirectToAction("index", "articles", new {id = uniqueAddress});
        }
    }
}