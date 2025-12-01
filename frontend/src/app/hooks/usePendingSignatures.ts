'use client';

import { useState, useEffect, useCallback } from 'react';
import Cookies from 'js-cookie';
import { jwtDecode } from 'jwt-decode';
import { toast } from 'react-toastify';
import { env } from 'next-runtime-env';

interface DecodedToken {
  role: string | string[];
  userId: string;
}

interface PendingUserDetail {
  userId: number;
  userName: string;
  userProfile: string;
  nameDocumentOwner: string;
  idDocumentOwner: number;
}

interface PendingDocumentDetail {
  documentId: number;
  documentName: string;
  userDetails: PendingUserDetail[];
  idDocumentOwner: number;
}

export interface PendingTcc {
  tccId: number;
  studentNames: string[];
  pendingDetails: PendingDocumentDetail[];
}

interface TccDocument {
  documentId: number;
  documentName: string;
  studentId: number;
}

interface UserTccGroup {
  tccId: number;
  studentNames: string[];
  documents: TccDocument[];
}

export interface GroupedByUserAndTcc {
  userId: number;
  userName: string;
  userProfile: string;
  tccGroups: UserTccGroup[];
}

export function usePendingSignatures() {
  const API_URL = env('NEXT_PUBLIC_API_URL');
  const [pendingData, setPendingData] = useState<PendingTcc[]>([]);
  const [groupedByUser, setGroupedByUser] = useState<GroupedByUserAndTcc[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [profile, setProfile] = useState<string | string[] | null>(null);
  const [userId, setUserId] = useState<string | null>(null);
  const [showAll, setShowAll] = useState(false);

  const fetchPendingSignatures = useCallback(async () => {
    setIsLoading(true);
    const token = Cookies.get('token');
    if (!token) {
      toast.error('Token não encontrado.');
      setIsLoading(false);
      return;
    }
    setIsLoading(true);
    setPendingData([]);

    const decoded = jwtDecode<DecodedToken>(token);
    if (!profile) setProfile(decoded.role);
    if (!userId) setUserId(decoded.userId);

    const canViewAll = Array.isArray(decoded.role)
      ? decoded.role.some((r) => ['ADMIN', 'SUPERVISOR'].includes(r))
      : ['ADMIN', 'SUPERVISOR'].includes(decoded.role);

    let endpoint = `${API_URL}/Signature/pending`;

    if (canViewAll && showAll) {
      // Busca todas as pendências
    } else {
      // Busca apenas as do usuário logado
      endpoint += `?userId=${decoded.userId}`;
    }

    try {
      const res = await fetch(endpoint, {
        headers: { Authorization: `Bearer ${token}` }
      });
      if (!res.ok) throw new Error('Falha ao buscar assinaturas pendentes.');
      const data = await res.json();
      setPendingData(data);
    } catch {
      toast.error('Erro ao carregar assinaturas pendentes.');
      setPendingData([]);
    } finally {
      setIsLoading(false);
    }
  }, [showAll, profile, userId, API_URL]);

  useEffect(() => {
    fetchPendingSignatures();
  }, [fetchPendingSignatures]);

  useEffect(() => {
    if (pendingData.length === 0) {
      setGroupedByUser([]);
      return;
    }

    const userMap = new Map<number, GroupedByUserAndTcc>();

    pendingData.forEach((tcc) => {
      tcc.pendingDetails.forEach((doc) => {
        doc.userDetails.forEach((user) => {
          if (!userMap.has(user.userId)) {
            userMap.set(user.userId, {
              userId: user.userId,
              userName: user.userName,
              userProfile: user.userProfile,
              tccGroups: []
            });
          }
          const userEntry = userMap.get(user.userId)!;

          let tccGroup = userEntry.tccGroups.find(
            (group) => group.tccId === tcc.tccId
          );
          if (!tccGroup) {
            tccGroup = {
              tccId: tcc.tccId,
              studentNames: tcc.studentNames,
              documents: []
            };
            userEntry.tccGroups.push(tccGroup);
          }

          tccGroup.documents.push({
            documentId: doc.documentId,
            documentName: doc.documentName,
            studentId: doc.idDocumentOwner
          });
        });
      });
    });

    setGroupedByUser(Array.from(userMap.values()));
  }, [pendingData, showAll]);

  return {
    groupedByUser,
    isLoading,
    profile,
    showAll,
    setShowAll,
    pendingData,
    userId
  };
}
