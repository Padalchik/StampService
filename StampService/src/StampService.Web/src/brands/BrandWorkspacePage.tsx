import { useEffect, useState } from 'react';
import { getApiErrorMessage } from '../api/errorMessages';
import { BrandSelector } from './BrandSelector';
import { BrandWorkspace } from './BrandWorkspace';
import {
  getBrandWorkspace,
  type BrandWorkspaceResponse,
  type MyBrandResponse
} from './brandWorkspaceApi';

type BrandWorkspacePageProps = {
  initialBrandId?: string;
  initialBrands?: MyBrandResponse[];
};

export function BrandWorkspacePage({
  initialBrandId,
  initialBrands
}: BrandWorkspacePageProps = {}) {
  const [workspace, setWorkspace] = useState<BrandWorkspaceResponse | null>(null);
  const [isWorkspaceLoading, setIsWorkspaceLoading] = useState(false);
  const [workspaceError, setWorkspaceError] = useState('');

  useEffect(() => {
    if (initialBrandId) {
      void openWorkspace(initialBrandId);
    }
  }, [initialBrandId]);

  async function openWorkspace(brandId: string) {
    setIsWorkspaceLoading(true);
    setWorkspaceError('');

    try {
      const response = await getBrandWorkspace(brandId);
      setWorkspace(response);
    } catch (requestError) {
      setWorkspace(null);
      setWorkspaceError(getUserMessage(requestError));
    } finally {
      setIsWorkspaceLoading(false);
    }
  }

  if (workspace) {
    return (
      <BrandWorkspace
        workspace={workspace}
        onWorkspaceUpdated={(updatedWorkspace) => setWorkspace(updatedWorkspace)}
        onBack={() => {
          setWorkspace(null);
          setWorkspaceError('');
        }}
      />
    );
  }

  return (
    <BrandSelector
      initialBrands={initialBrands}
      isOpeningBrand={isWorkspaceLoading}
      workspaceError={workspaceError}
      onOpenBrand={(brandId) => void openWorkspace(brandId)}
    />
  );
}

function getUserMessage(error: unknown): string {
  return getApiErrorMessage(error, 'Не удалось выполнить запрос.');
}
