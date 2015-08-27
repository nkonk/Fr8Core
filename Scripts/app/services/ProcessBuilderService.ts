/// <reference path="../_all.ts" />

/*
    The service enables operations with Process Templates
*/
module dockyard.services {
    export interface IProcessTemplateService extends ng.resource.IResourceClass<interfaces.IProcessTemplateVM> { }
    export interface IActionService extends ng.resource.IResourceClass<interfaces.IActionVM> {
        getConfigurationSettings: (actionRegistrationId: { actionRegistrationId: number }) => ng.resource.IResource<string>, string;
    }
    export interface IDocuSignTemplateService extends ng.resource.IResourceClass<interfaces.IDocuSignTemplateVM> { }
    export interface IDocuSignTriggerService extends ng.resource.IResourceClass<interfaces.IDocuSignExternalEventVM> { }

    app.factory('ProcessTemplateService', ['$resource', 'urlPrefix', ($resource: ng.resource.IResourceService, urlPrefix: string): IProcessTemplateService =>
        <IProcessTemplateService> $resource(urlPrefix + '/processTemplate/:id', { id: '@id' })
    ]);

    app.factory('DocuSignTemplateService', ['$resource', 'urlPrefix', ($resource: ng.resource.IResourceService, urlPrefix: string): IDocuSignTemplateService =>
        <IDocuSignTemplateService> $resource(urlPrefix + '/docusigntemplate')
    ]);

    app.factory('DocuSignTriggerService', ['$resource', 'urlPrefix', ($resource: ng.resource.IResourceService, urlPrefix: string): IDocuSignTriggerService =>
        <IDocuSignTriggerService> $resource('/apimocks/processtemplate/triggersettings')
    ]);

    app.factory('ActionService', ['$resource', 'urlPrefix', ($resource: ng.resource.IResourceService, urlPrefix: string): IActionService =>
        <IActionService> $resource(urlPrefix + '/Action/:id',
            {
                id: '@id'
            },
            {
                'save': {
                    method: 'POST',
                    isArray: true
                },
                'delete': { method: 'DELETE' },
                'getConfigurationSettings': {
                    method: 'GET',
                    url: urlPrefix + '/Action/configuration/?:curActionRegistrationId',
                    params: {
                        curActionRegistrationId: '@actionRegistrationId'
                    }
                }
            })
    ]);
}