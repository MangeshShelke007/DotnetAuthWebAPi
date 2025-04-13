using AutoMapper;
using System;
using DotnetAuthWebAPi.Domain.Contract;
using DotnetAuthWebAPi.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace DotnetAuthWebAPi.Service
{
    public class UserService : IUserServices
    {
        private readonly ITokenService _tokenService;
        private readonly ICurrentUserService _currentUserService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;
        private readonly ILogger<UserService> _logger;

        public UserService(ITokenService tokenService, ICurrentUserService currentUserService, 
            UserManager<ApplicationUser> userManager, IMapper mapper, ILogger<UserService> logger)
        {
            _tokenService = tokenService;
            _currentUserService = currentUserService;
            _userManager = userManager;
            _mapper = mapper;
            _logger = logger;
        }
        //method to register new user
        public async Task<UserResponse> RegisterAsync(UserRegisterRequest request)
        {
            _logger.LogInformation("Registering User");
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if(existingUser != null)
            {
                _logger.LogInformation("Email already exists");
                throw new System.Exception("Email already exists");
            }
            var newUser = _mapper.Map<ApplicationUser>(request);

            //Generate a unique username
            newUser.UserName = GenerateUserName(request.FirstName, request.LastName);
            newUser.CrateAt = DateTime.Now;
            var result = await _userManager.CreateAsync(newUser, request.Password);
            if(!result.Succeeded)
            {
                var errors = string.Join(",", result.Errors.Select(e => e.Description));
                _logger.LogError("Failed to create user:{error}",errors);
                throw new System.Exception($"Failed to create user:{errors}");
            }
            _logger.LogInformation("User created successfully");
            await _tokenService.GenerateToken(newUser);
            newUser.CrateAt= DateTime.Now;
             var userResponse=_mapper.Map<UserResponse>(newUser);
            userResponse.CreateAt = DateTime.Now;
            return userResponse;
            

        }
        //method to login user
        public async Task<UserResponse> LoginAsync(UserLoginRequest request)
        {
           if(request==null)
            {
                _logger.LogInformation("Login Request is null");
                throw new ArgumentNullException(nameof(request));
            }
            var user = await _userManager.FindByEmailAsync(request.Email);
            if(user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
            {
                _logger.LogError("Invalid email or password");
                throw new System.Exception("Invalid email or password");
            }
            //Generate access token
            var token = await _tokenService.GenerateToken(user);

            //Generate refresh token
            var refreshToken = _tokenService.GenerateRefreshToken();

            //Hash refresh token and store it in the database or override
            //the refresh token
            using var sha256 = SHA256.Create();

            var refreshTokenHash= sha256.ComputeHash(Encoding.UTF8.GetBytes(refreshToken));
            user.RefreshToken= Convert.ToBase64String(refreshTokenHash);
            user.RefreshTokenExpiryTime = DateTime.Now.AddDays(2);
            user.CrateAt=DateTime.Now;

            //update user information in database
            var result = await _userManager.UpdateAsync(user);
            if(!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(x => x.Description));
                _logger.LogInformation($"Failed to update user:{errors}", errors);
                throw new System.Exception($"Failed to update user:{errors}");
            }
            var userResponse = _mapper.Map<ApplicationUser,UserResponse>(user);
            userResponse.CreateAt = DateTime.Now;
            userResponse.AccessToken= token;
            userResponse.RefreshToken=refreshToken;

            return userResponse;
           
        }
        //Get user by id
        public async Task<UserResponse> GetByIdAsync(Guid id)
        {
            _logger.LogInformation("Getting User by Id");
            var user = await _userManager.FindByIdAsync(id.ToString());
            if(user == null) {
                _logger.LogError("User not found");
                throw new System.Exception("User not found");
            }
            _logger.LogInformation("User found");
            return _mapper.Map<UserResponse>(user);
;
        }
        public async Task<CurrentUserResponse> GetCurrentUserAsync()
        {
            var user = await _userManager.FindByIdAsync(_currentUserService.GetUserId());
            if(user == null) {
                _logger.LogError("User not found");
                throw new System.Exception("User not found");
            }
            return _mapper.Map<CurrentUserResponse>(user);  
        }

        public async Task<CurrentUserResponse> RefreshTokenAsync(RefreshTokenRequest request)
        {
            _logger.LogInformation("Refresh token");

            //hash the incoming refreshtoken and compare it with the one stored
            //in the database

            using var sha256 = SHA256.Create();
            var refreshTokenHash = sha256.ComputeHash(Encoding.UTF8.GetBytes(request.RefreshToken));
            var hashedRefreshToken = Convert.ToBase64String(refreshTokenHash);


            //find user based on refresh token
            var user = await _userManager.Users.FirstOrDefaultAsync(x => x.RefreshToken == hashedRefreshToken);

            if(user == null)
            {
                _logger.LogError("Invalid refresh token");
                throw new System.Exception("Invalid refresh token");
            }

            //validate the token refresh expiry time
            if(user.RefreshTokenExpiryTime<DateTime.Now)
            {
                _logger.LogWarning("Refresh Token Expired for UserId: {UserId}",user.Id);
                throw new System.Exception("Refresh token Expired");
            }

            //Generate a access token
            var newAccessToken = await _tokenService.GenerateToken(user);
            _logger.LogInformation("Access token generated successfully");

            var currenUserResponse = _mapper.Map<CurrentUserResponse>(user);
            currenUserResponse.AccessToken = newAccessToken;
            return currenUserResponse;
        }
        public async Task<UserResponse> UpdateAsync(Guid id, UpdateUserRequest request)
        {
            var user = await  _userManager.FindByIdAsync(id.ToString());
            if(user == null)
            {
                _logger.LogError("User not found");
                throw new System.Exception("User not found");
            }
            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.Email = request.Email;
            user.UpdateAt = DateTime.Now;   

            await _userManager.UpdateAsync(user);
            return _mapper.Map<UserResponse>(user);
        }
        public async Task DeleteAsync(Guid id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if(user == null) {
                _logger.LogError("User Not found");
                throw new System.Exception("User not found");
            }
            await _userManager.DeleteAsync(user);
        }
        public async Task<RevokeRefreshTokenResponse> RevokeRefreshToken(RefreshTokenRequest refreshTokenRemoveRequest)
        {
            _logger.LogInformation("Revoking refresh token");
            try
            {
                using var sha256 = SHA256.Create();
                var refreshTokenHash = sha256.ComputeHash(Encoding.UTF8.GetBytes(refreshTokenRemoveRequest.RefreshToken));
                var hashedRefreshToken = Convert.ToBase64String(refreshTokenHash);

                //Find the user base on refresh token

                var user = await _userManager.Users.FirstOrDefaultAsync(x => x.RefreshToken == hashedRefreshToken);
                if(user==null)
                {
                    _logger.LogError("Invalid refresh token");
                    throw new System.Exception("Invalid refresh token");
                }
                if (user.RefreshTokenExpiryTime == DateTime.Now)
                {
                    _logger.LogWarning("Invalid refresh token");
                    throw new System.Exception("Invalid refresh token");
                }
                //Remove refersh token
                user.RefreshToken = null;
                user.RefreshTokenExpiryTime = null;

                //update user information in database
                var result = await _userManager.UpdateAsync(user);
                if(!result.Succeeded)
                {
                    _logger.LogError("Failed to update user");
                    
                        return new RevokeRefreshTokenResponse
                        {
                            Message = "Failed to revoke refresh tokne"
                        };
                    
                }
                _logger.LogInformation("Refresh token revoked successfully");
                return new RevokeRefreshTokenResponse
                {
                    Message = "Refresh token revoked successfully"
                };
            }
            catch (System.Exception ex)
            {
                _logger.LogError("Failed to revoke refresh token:{ex}", ex.Message);
                throw new System.Exception("Failed to revok refresh token");
            }
        }
        private string GenerateUserName(string firstName, string lastName)
        {
            var baseUserName = $"{firstName}{lastName}".ToLower();

            //check if username already exists

            var userName = baseUserName;
            var count = 1;
            while(_userManager.Users.Any(x=>x.UserName == userName))
            {
                userName = $"{baseUserName}{count}";
                count++;
            }
            return userName;    
        }

   
      
     

 

     
      

 

   
    }
}
