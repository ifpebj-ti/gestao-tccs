import { SubmitHandler, useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { newUserSchema, NewUserSchemaSchemaType } from '@/app/schemas/newUserSchema';
import { toast } from 'react-toastify';

export function useNewUserForm() {
  const form = useForm<NewUserSchemaSchemaType>({
    resolver: zodResolver(newUserSchema),
    defaultValues: {
      name: '',
      email: '',
      registration: '',
      cpf: '',
      profile: 'STUDENT',
      siape: '',
      course: 'ENGENHARIA DE SOFTWARE'
    }
  });

  const submitForm: SubmitHandler<NewUserSchemaSchemaType> = async (data) => {
    try {
      const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/User`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify(data)
      });

      console.log('Dados enviados:', data);
      console.log('Resposta do servidor:', response);
      if (response.ok) {
        toast.success('Usuário registrado com sucesso!');
        form.reset();
        window.location.href = '/newPassword';
      } else {
        const errorText = await response.text();
        toast.error(`Erro: ${response.status} - ${errorText}`);
      }
    } catch (error) {
      toast.error('Erro ao registrar usuário. Tente novamente mais tarde.');
      console.error('Erro no cadastro:', error);
    }
  };

  return { form, submitForm };
}
