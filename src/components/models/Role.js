import { Model } from 'objection';

class Role extends Model {
    static tableName = 'roles';
}

const defaultRoleName = 'Contributer';

// Hardcoded role IDs for manager and contributor roles
// Must update these if the DB ever gets updated
const managerRoleId = 3;
const contributorRoleId = 4;
const techSupportRoleId = 6;

export { Role as default, defaultRoleName, managerRoleId, contributorRoleId, techSupportRoleId };
