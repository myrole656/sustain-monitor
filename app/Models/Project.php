<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Factories\HasFactory;

class Project extends Model
{
    use HasFactory;

    protected $fillable = [
        'project_name',
        'project_location',
        'reg_date',
        'pic_name',
        'pic_contact',
        'target',
        'status',
        'user_id',
    ];

    public function process()
    {
        return $this->hasOne(Process::class);
    }
     public function sdgStatus()
    {
        return $this->hasOne(SDGStatus::class, 'project_id', 'id');
    }

    public function user()
    {
        return $this->belongsTo(User::class);
    }
    
}
