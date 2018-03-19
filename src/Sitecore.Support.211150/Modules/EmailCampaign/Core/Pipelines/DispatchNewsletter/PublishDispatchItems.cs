namespace Sitecore.Support.Modules.EmailCampaign.Core.Pipelines.DispatchNewsletter
{
    using Sitecore.Analytics;
    using Sitecore.Analytics.Automation.Data.Items;
    using Sitecore.Data;
    using Sitecore.Data.Items;
    using Sitecore.Diagnostics;
    using Sitecore.ExM.Framework.Diagnostics;
    using Sitecore.Modules.EmailCampaign;
    using Sitecore.Modules.EmailCampaign.Core;
    using Sitecore.Modules.EmailCampaign.Factories;
    using Sitecore.Modules.EmailCampaign.Messages;

    public class PublishDispatchItems : Sitecore.Modules.EmailCampaign.Core.Pipelines.DispatchNewsletter.PublishDispatchItems
    {
        private readonly ILogger logger = EcmFactory.GetDefaultFactory().Io.Logger;
        protected override IPublishingTask PublishMessage(MessageItem message)
        {
            Assert.ArgumentNotNull(message, "message");

            var publishingTask = new Sitecore.Support.Modules.EmailCampaign.Core.PublishingTask(message.InnerItem, this.logger)
            {
                PublishRelatedItems = true
            };
            publishingTask.PublishAsync();

            return publishingTask;
        }

        protected override IPublishingTask PublishCampaign(MessageItem message)
        {
            Assert.ArgumentNotNull(message, "message");

            var messageCampaignId = message.CampaignId;
            if (ID.IsNullOrEmpty(messageCampaignId))
            {
                messageCampaignId = Factory.GetMessage(message.ID).CampaignId;
            }

            Item campaignItem = this.ItemUtilExt.GetItem(messageCampaignId);
            if (campaignItem == null || campaignItem.TemplateID != AnalyticsIds.Campaign)
            {
                return null;
            }

            var publishingTask = new Sitecore.Support.Modules.EmailCampaign.Core.PublishingTask(campaignItem, this.logger);
            publishingTask.PublishAsync();

            return publishingTask;
        }

        protected override IPublishingTask PublishPlan(MessageItem message)
        {
            Assert.ArgumentNotNull(message, "message");

            Item planItem = this.ItemUtilExt.GetItem(message.PlanId);
            if (planItem == null || planItem.TemplateID != EngagementPlanItem.TemplateID)
            {
                return null;
            }

            var publishingTask = new Sitecore.Support.Modules.EmailCampaign.Core.PublishingTask(planItem, this.logger);
            publishingTask.PublishAsync();

            return publishingTask;
        }
    }
}