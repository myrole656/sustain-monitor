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
        'pic_contact',
        'target',
        'user_id',
    ];

    public function process()
    {
        return $this->hasOne(Process::class);
    }

    public function user()
    {
        return $this->belongsTo(User::class);
    }
    protected static function booted()
{
    static::updated(function ($project) {
        if ($project->isDirty('status')) { // Only when status changes
            $project->user->notify(
                new \App\Notifications\ProjectStatusNotification($project->name, $project->status)
            );
        }
    });
}
}
