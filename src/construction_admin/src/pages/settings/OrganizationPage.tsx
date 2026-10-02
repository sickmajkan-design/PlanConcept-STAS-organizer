import { AccountTreeOutlined, BusinessOutlined } from '@mui/icons-material';
import { Box, Tab, Tabs } from '@mui/material';
import { useSearchParams } from 'react-router-dom';

import { PageHeader } from '../../components/PageHeader';
import { useBranchesQuery } from '../../features/branches/useBranches';
import { useT } from '../../i18n/useI18n';
import { BranchesPanel } from './BranchesPage';
import { CompanyProfilePanel } from './CompanySettingsPage';

type Tab = 'company' | 'branches';

/**
 * The company and its business units (poslovne jedinice) in one window: the company is the
 * parent — its profile and tax numbers — and the units are its parts, each with data of its own.
 * Both used to be separate pages; the old addresses now land here on the matching tab.
 */
export function OrganizationPage() {
  const t = useT();
  const [params, setParams] = useSearchParams();
  const { data: branches } = useBranchesQuery();

  const tab: Tab = params.get('tab') === 'branches' ? 'branches' : 'company';

  const choose = (next: Tab) => {
    const copy = new URLSearchParams(params);
    copy.set('tab', next);
    setParams(copy, { replace: true });
  };

  return (
    <Box>
      <PageHeader title={t('organization.title')} description={t('organization.description')} />

      <Tabs
        value={tab}
        onChange={(_event, value: Tab) => choose(value)}
        sx={{ mb: 3, borderBottom: 1, borderColor: 'divider' }}
      >
        <Tab
          value="company"
          icon={<BusinessOutlined fontSize="small" />}
          iconPosition="start"
          label={t('organization.tabCompany')}
        />
        <Tab
          value="branches"
          icon={<AccountTreeOutlined fontSize="small" />}
          iconPosition="start"
          label={branches?.length ? `${t('organization.tabBranches')} (${branches.length})` : t('organization.tabBranches')}
        />
      </Tabs>

      {tab === 'company' ? <CompanyProfilePanel /> : <BranchesPanel />}
    </Box>
  );
}
