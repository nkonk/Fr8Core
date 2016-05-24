using System;
using System.Collections.Generic;
using System.Linq;
using Data.Interfaces.Manifests;
using Fr8Data.Crates;
using Fr8Data.DataTransferObjects;
using Fr8Data.Helpers;
using Fr8Data.Manifests;
using TerminalBase.BaseClasses;

namespace TerminalBase.Infrastructure
{
    public class EnhancedValidationManager<TActivityUi> : ValidationManager where TActivityUi : StandardConfigurationControlsCM
    {
        private readonly TActivityUi _activityUi;

        private readonly Dictionary<IControlDefinition, string> _ownerNameByControl; 
        public EnhancedValidationManager(ValidationResultsCM validationResults, EnhancedTerminalActivity<TActivityUi> activity,  ICrateStorage payload) : base(validationResults, payload)
        {
            if (activity == null)
            {
                throw new ArgumentNullException(nameof(activity));
            }
            _activityUi = activity.ConfigurationControls;
            _ownerNameByControl = new Dictionary<IControlDefinition, string>();
            FillOwnerByControl();
        }

        private void FillOwnerByControl()
        {
            foreach (var collectionProperty in Fr8ReflectionHelper.GetMembers(typeof(TActivityUi)).Where(x => x.CanRead
                                                                                                      && x.GetCustomAttribute<DynamicControlsAttribute>() != null
                                                                                                      && Fr8ReflectionHelper.CheckIfMemberIsCollectionOf<IControlDefinition>(x)))
            {
                var collection = collectionProperty.GetValue(_activityUi) as IEnumerable<IControlDefinition>;
                if (collection == null)
                {
                    continue;
                }
                foreach (var control in collection)
                {
                    _ownerNameByControl.Add(control, collectionProperty.Name);
                }
            }
        }

        protected override string ResolveControlName(ControlDefinitionDTO control)
        {
            string ownerName;
            return _ownerNameByControl.TryGetValue(control, out ownerName)
                       ? ActivityHelper.GetDynamicControlName(control.Name, ownerName)
                       : base.ResolveControlName(control);
        }
    }
}