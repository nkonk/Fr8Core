var dockyard;
(function (dockyard) {
    var tests;
    (function (tests) {
        var utils;
        (function (utils) {
            //The class contains methods to create mocks for complex objects
            var Factory = (function () {
                function Factory() {
                }
                //Creates a mock for ProcessBuilderController $scope
                Factory.GetProcessBuilderScope = function (rootScope) {
                    var scope = rootScope.$new();
                    scope.processTemplateId = 0;
                    scope.processNodeTemplates = null;
                    scope.fields = null;
                    return scope;
                };
                return Factory;
            })();
            utils.Factory = Factory;
            /*
                Mock for ActionService
            */
            var ActionServiceMock = (function () {
                function ActionServiceMock($q) {
                    this.save = jasmine.createSpy('save').and.callFake(function () {
                        var def = $q.defer();
                        def.resolve();
                        def.promise.$promise = def.promise;
                        return def.promise;
                    });
                    this.get = jasmine.createSpy('get');
                }
                return ActionServiceMock;
            })();
            utils.ActionServiceMock = ActionServiceMock;
        })(utils = tests.utils || (tests.utils = {}));
    })(tests = dockyard.tests || (dockyard.tests = {}));
})(dockyard || (dockyard = {}));
//# sourceMappingURL=mocks.js.map