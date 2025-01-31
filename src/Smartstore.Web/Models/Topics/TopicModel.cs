﻿using System;
using Smartstore.Core.Localization;
using Smartstore.Web.Modelling;

namespace Smartstore.Web.Models.Topics
{
    public partial class TopicModel : EntityModelBase
    {
        public string SystemName { get; set; }

        public string HtmlId { get; set; }

        public string BodyCssClass { get; set; }

        public bool IncludeInSitemap { get; set; }

        public bool IsPasswordProtected { get; set; }

        public LocalizedValue<string> ShortTitle { get; set; }

        public LocalizedValue<string> Title { get; set; }

        public LocalizedValue<string> Intro { get; set; }

        public LocalizedValue<string> Body { get; set; }

        public string MetaKeywords { get; set; }

        public string MetaDescription { get; set; }

        public string MetaTitle { get; set; }

        public string SeName { get; set; }

        public string CanonicalUrl { get; set; }

        public string TitleTag { get; set; }

        public bool RenderAsWidget { get; set; }
    }
}