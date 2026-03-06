// Purpose: Stores active runtime unit views in a central model for manager/service queries.
using System.Collections.Generic;
using WOR.Gameplay.Core.Mvp;

namespace WOR.Gameplay.Modules.Units.Models {

    public sealed class UnitRegistryModel : IModel {

        private readonly HashSet<UnitActions> _registeredUnits = new HashSet<UnitActions>();

        public IEnumerable<UnitActions> RegisteredUnits => _registeredUnits;

        public bool Register(UnitActions unit) {
            if(unit == null) {
                return false;
            }

            return _registeredUnits.Add(unit);
        }

        public bool Unregister(UnitActions unit) {
            if(unit == null) {
                return false;
            }

            return _registeredUnits.Remove(unit);
        }

        public void CleanupDestroyedUnits() {
            _registeredUnits.RemoveWhere(unit => unit == null || unit.gameObject == null);
        }

        public void Clear() {
            _registeredUnits.Clear();
        }
    }
}
