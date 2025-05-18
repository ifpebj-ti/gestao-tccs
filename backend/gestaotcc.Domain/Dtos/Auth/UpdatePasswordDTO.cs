using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gestaotcc.Domain.Dtos.Auth;
public record UpdatePasswordDTO(string UserEmail, string UserPassword);
