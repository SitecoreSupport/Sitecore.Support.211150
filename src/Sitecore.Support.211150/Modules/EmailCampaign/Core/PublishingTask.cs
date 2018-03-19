using Sitecore.Globalization;

namespace Sitecore.Support.Modules.EmailCampaign.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Sitecore.Configuration;
    using Sitecore.Data;
    using Sitecore.Data.Items;
    using Sitecore.Diagnostics;
    using Sitecore.Modules.EmailCampaign.Core.Extensions;
    using Sitecore.ExM.Framework.Diagnostics;
    using Sitecore.Publishing;
    using Sitecore.SecurityModel;
    using Sitecore.Modules.EmailCampaign.Core;

    public class PublishingTask : IPublishingTask
    {
        private readonly Item _dataItem;
        private readonly ILogger _logger;
        private bool _published;
        private Handle _handle;

        public PublishingTask([NotNull]Item item, [NotNull] ILogger logger)
        {
            Assert.ArgumentNotNull(item, "item");
            Assert.ArgumentNotNull(logger, "logger");
            Assert.IsNotNull(item, "item");
            this._dataItem = item;
            this._logger = logger;
        }

        public bool PublishRelatedItems { get; set; }

        public void PublishAsync()
        {
            if (this._published)
            {
                throw new InvalidOperationException("The item " + this._dataItem.ID + " is already published");
            }

            this._published = true;

            var targets = this.GetTargets(this._dataItem).ToArray();

            Item unpublishedParentFolder = this.FindUnpublishedParentFolder(this._dataItem, targets);
            Item itemToPublish = unpublishedParentFolder ?? this._dataItem;

            _handle = PublishManager.PublishItem(
              itemToPublish,
              targets,
              GetLanguages(itemToPublish),
              true /* deep */,
              true /* compareRevisions */,
              PublishRelatedItems /* publishRelatedItems */);
        }

        public void WaitForCompletion()
        {
            PublishManager.WaitFor(_handle);
        }

        /// <summary>
        /// Gets the targets.
        /// </summary>
        /// <param name="item">The item to publish.</param>
        /// <returns>
        /// The targets.
        /// </returns>
        private IEnumerable<Database> GetTargets(Item item)
        {
            using (new SecurityDisabler())
            {
                var publishingTargetsItem = item.Database.GetItem("/sitecore/system/publishing targets");
                if (publishingTargetsItem == null)
                {
                    yield break;
                }

                foreach (Item baseItem in publishingTargetsItem.Children)
                {
                    string targetDatabase = baseItem["Target database"];
                    if (string.IsNullOrEmpty(targetDatabase))
                    {
                        continue;
                    }

                    Database database = Factory.GetDatabase(targetDatabase, false);

                    if (database != null)
                    {
                        yield return database;
                    }
                    else
                    {
                        this._logger.LogWarn("Unknown database in PublishAction: " + targetDatabase);
                    }
                }
            }
        }

        [CanBeNull]
        private Item FindUnpublishedParentFolder([NotNull] Item item, [NotNull] Database[] targets)
        {
            Assert.ArgumentNotNull(item, "item");
            Assert.ArgumentNotNull(targets, "targets");
            Item unpublishedParentFolder = null;
            foreach (var folderItem in item.GetParentItems(new TemplateID(TemplateIDs.Folder), new TemplateID(TemplateIDs.MediaFolder)))
            {
                if (this.IsItemPublished(folderItem, targets))
                {
                    break;
                }

                unpublishedParentFolder = folderItem;
            }

            return unpublishedParentFolder;
        }

        private bool IsItemPublished(Item item, Database[] targets)
        {
            return targets.All(t => t.GetItem(item.ID) != null);
        }

        private Language[] GetLanguages(Item item)
        {
            return item.Database.Languages;
        }
    }
}