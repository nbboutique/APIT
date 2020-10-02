﻿using System;
using System.Threading.Tasks;
using BusinessLayer.Models;
using DatabaseLayer.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Apit.Controllers
{
    public partial class ArticlesController
    {
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost, Authorize]
        public async Task<IActionResult> Create(NewArticleViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            var dateNow = DateTime.Now;

            var topic = model.CreateNewTopic && !_dataManager.Topics.IsExist(model.Topic)
                ? new Topic {Id = Guid.NewGuid(), Name = model.Topic}
                : _dataManager.Topics.GetByName(model.Topic);

            if (model.CreateNewTopic) _dataManager.Topics.Create(topic);

            if (model.UseFromFile)
            {
                Console.WriteLine("======= UseFromFile IS TRUE =======");
                throw new NotImplementedException();
                //TODO: Create article by MS Word Document uploaded file by user
            }
            else
            {
                _dataManager.Articles.Create(new ArticleViewModel
                {
                    Id = Guid.NewGuid().ToString(),
                    Topic = topic,
                    Creator = user,

                    DateCreated = dateNow,
                    DateLastModified = dateNow,
                    KeyWords = model.KeyWords.Split(' ', ',', ';'),

                    Title = model.Title,
                    HTML = string.IsNullOrWhiteSpace(model.TextHTML) ? " ==== Empty ====" : model.TextHTML
                });
            }

            return Redirect("/articles/list");
        }

        [Authorize]
        public IActionResult Edit(string id)
        {
            var model = _dataManager.Articles.GetById(Guid.Parse(id));
            return View(model);
        }

        [HttpPost, Authorize]
        public async Task<IActionResult> Edit(ArticleViewModel model)
        {
            //TODO: Edit page response 
            // var user = await _userManager.GetUserAsync(User);
            // if (model.Creator != user)
            //     ModelState.AddModelError(nameof(ArticleViewModel.Creator),
            //         "User access denied");

            return View();
        }

        [HttpPost, Authorize]
        public async Task<IActionResult> Delete(string id, string returnUrl = null)
        {
            var articleId = Guid.Parse(id);
            if (_dataManager.Articles.IsExist(articleId))
            {
                var article = _dataManager.Articles.GetById(articleId);
                var user = await _userManager.GetUserAsync(User);

                if (user == article.Creator)
                {
                    _dataManager.Articles.Delete(articleId);
                }
                else
                {
                    ModelState.AddModelError(nameof(ArticleViewModel.Creator), "User access denied");
                }
            }
            else
                ModelState.AddModelError(nameof(ArticleViewModel.Id), "Article not exist");

            return Redirect(returnUrl ?? "/");
        }
    }
}